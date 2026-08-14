using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SupportTicketManagement.Core.DTO;
using SupportTicketManagement.Core.Enums;
using SupportTicketManagement.Core.ServiceContracts;
using System.Security.Claims;

namespace SupportTicketManagement.API.Controllers
{
    /// <summary>
    /// Controller responsible for managing support tickets.
    /// Provides endpoints for creating, retrieving, updating, and assigning tickets.
    /// Uses role-based logic to isolate data appropriately (Admin, SupportAgent, Customer).
    /// </summary>
    [ApiVersion(1.0)]
    public class TicketsController : CustomControllerBase
    {
        private readonly ITicketsService _ticketsService;

        public TicketsController(ITicketsService ticketsService)
        {
            _ticketsService = ticketsService;
        }

        private Guid GetUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
        }

        private bool IsAdmin() => User.IsInRole(UserRole.Admin.ToString());
        private bool IsSupportAgent() => User.IsInRole(UserRole.SupportAgent.ToString());

        /// <summary>
        /// Retrieves a paginated list of tickets.
        /// Results are filtered based on the user's role:
        /// Admins see all tickets, Agents see only assigned tickets, Customers see only their own tickets.
        /// </summary>
        /// <param name="request">Pagination, filtering, and sorting parameters.</param>
        /// <returns>A paginated list of tickets.</returns>
        [Authorize]
        [HttpGet]
        [ProducesResponseType(typeof(PaginatedResponse<TicketResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetTickets([FromQuery] TicketQueryRequest request)
        {
            ApiResponse response;
            var userId = GetUserId();

            if (IsAdmin())
            {
                response = await _ticketsService.GetTicketsForAdminAsync(request);
            }
            else if (IsSupportAgent())
            {
                response = await _ticketsService.GetTicketsForAgentAsync(request, userId);
            }
            else
            {
                response = await _ticketsService.GetTicketsForCustomerAsync(request, userId);
            }

            if (!response.Success)
                return ToActionResult(response);

            var responseData = response as ApiResponseWithData<PaginatedResponse<TicketResponse>>;
            return Ok(responseData?.Data);
        }

        /// <summary>
        /// Retrieves a specific ticket by its ID.
        /// Access is restricted based on the user's role and ownership/assignment of the ticket.
        /// </summary>
        /// <param name="id">The unique identifier of the ticket.</param>
        /// <returns>The ticket details.</returns>
        [Authorize]
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(TicketResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetTicketById(Guid id)
        {
            ApiResponse response;
            var userId = GetUserId();

            if (IsAdmin())
            {
                response = await _ticketsService.GetTicketByIdForAdminAsync(id);
            }
            else if (IsSupportAgent())
            {
                response = await _ticketsService.GetTicketByIdForAgentAsync(id, userId);
            }
            else
            {
                response = await _ticketsService.GetTicketByIdForCustomerAsync(id, userId);
            }

            if (!response.Success)
                return ToActionResult(response);

            var responseData = response as ApiResponseWithData<TicketResponse>;
            return Ok(responseData?.Data);
        }

        /// <summary>
        /// Creates a new support ticket.
        /// Only users with the Customer role can create tickets.
        /// </summary>
        /// <param name="request">The ticket details (Title, Description, Priority).</param>
        /// <returns>The newly created ticket.</returns>
        [HttpPost]
        [Authorize(Roles = nameof(UserRole.Customer))]
        [ProducesResponseType(typeof(TicketResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> CreateTicket([FromBody] CreateTicketRequest request)
        {
            var userId = GetUserId();
            var response = await _ticketsService.CreateTicketAsync(request, userId);

            if (!response.Success)
                return ToActionResult(response);

            var responseData = response as ApiResponseWithData<TicketResponse>;
            return Ok(responseData?.Data);
        }

        /// <summary>
        /// Updates the core details (Title, Description) of a ticket.
        /// Only users with the Admin role can perform this general update.
        /// </summary>
        /// <param name="id">The unique identifier of the ticket.</param>
        /// <param name="request">The updated ticket details.</param>
        /// <returns>Success response.</returns>
        [HttpPut("{id:guid}")]
        [Authorize(Roles = nameof(UserRole.Admin))]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateTicket(Guid id, [FromBody] UpdateTicketRequest request)
        {
            var userId = GetUserId();
            var response = await _ticketsService.UpdateTicketAsync(id, request, userId);
            return ToActionResult(response);
        }

        /// <summary>
        /// Updates the status of a ticket.
        /// Role rules apply:
        /// - Admin: Can set any status.
        /// - SupportAgent: Can resolve assigned tickets.
        /// - Customer: Can close or reopen their own tickets.
        /// </summary>
        /// <param name="id">The unique identifier of the ticket.</param>
        /// <param name="request">The new status.</param>
        /// <returns>Success response.</returns>
        [Authorize]
        [HttpPatch("{id:guid}/status")]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateTicketStatus(Guid id, [FromBody] UpdateTicketStatusRequest request)
        {
            ApiResponse response;
            var userId = GetUserId();

            if (IsAdmin())
            {
                response = await _ticketsService.UpdateTicketStatusByAdminAsync(id, request, userId);
            }
            else if (IsSupportAgent())
            {
                response = await _ticketsService.UpdateTicketStatusByAgentAsync(id, request, userId);
            }
            else
            {
                response = await _ticketsService.UpdateTicketStatusByCustomerAsync(id, request, userId);
            }

            return ToActionResult(response);
        }

        /// <summary>
        /// Updates the priority of a ticket.
        /// Only users with the Admin role can change ticket priority.
        /// </summary>
        /// <param name="id">The unique identifier of the ticket.</param>
        /// <param name="request">The new priority.</param>
        /// <returns>Success response.</returns>
        [HttpPatch("{id:guid}/priority")]
        [Authorize(Roles = nameof(UserRole.Admin))]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateTicketPriority(Guid id, [FromBody] UpdateTicketPriorityRequest request)
        {
            var userId = GetUserId();
            var response = await _ticketsService.UpdateTicketPriorityAsync(id, request, userId);
            return ToActionResult(response);
        }

        /// <summary>
        /// Assigns a Support Agent to a ticket.
        /// Only users with the Admin role can assign agents.
        /// </summary>
        /// <param name="id">The unique identifier of the ticket.</param>
        /// <param name="request">The ID of the Support Agent.</param>
        /// <returns>Success response.</returns>
        [HttpPatch("{id:guid}/assign")]
        [Authorize(Roles = nameof(UserRole.Admin))]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> AssignAgent(Guid id, [FromBody] AssignTicketRequest request)
        {
            var userId = GetUserId();
            var response = await _ticketsService.AssignAgentAsync(id, request, userId);
            return ToActionResult(response);
        }
    }
}
