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
    /// Controller responsible for managing time entries on a specific support ticket.
    /// Provides endpoints for retrieving time logs and allowing assigned agents to log their work duration.
    /// </summary>
    [ApiVersion(1.0)]
    [Route("api/v{version:apiVersion}/tickets/{ticketId:guid}/time-entries")]
    public class TicketTimeEntriesController : CustomControllerBase
    {
        private readonly ITimeEntriesService _timeEntriesService;

        public TicketTimeEntriesController(ITimeEntriesService timeEntriesService)
        {
            _timeEntriesService = timeEntriesService;
        }

        private Guid GetUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
        }

        private string GetUserRole()
        {
            return User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;
        }

        /// <summary>
        /// Retrieves all time entries logged for a specific ticket.
        /// Access is restricted to Admins and the Agent explicitly assigned to the ticket.
        /// Customers cannot view time entries.
        /// </summary>
        /// <param name="ticketId">The unique identifier of the ticket.</param>
        /// <returns>A list of time entries along with the total duration logged.</returns>
        [HttpGet]
        [Authorize(Roles = $"{nameof(UserRole.SupportAgent)},{nameof(UserRole.Admin)}")]
        [ProducesResponseType(typeof(TicketTimeEntriesResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetTimeEntries(Guid ticketId)
        {
            var response = await _timeEntriesService.GetTimeEntriesAsync(ticketId, GetUserId(), GetUserRole());
            
            if (!response.Success)
                return ToActionResult(response);

            var responseData = response as ApiResponseWithData<TicketTimeEntriesResponse>;
            return Ok(responseData?.Data);
        }

        /// <summary>
        /// Logs a new time entry for a specific ticket.
        /// Only the Support Agent currently assigned to the ticket can log time.
        /// </summary>
        /// <param name="ticketId">The unique identifier of the ticket.</param>
        /// <param name="request">The time entry details (WorkDate, DurationMinutes, Description).</param>
        /// <returns>A success response if the time entry is added.</returns>
        [HttpPost]
        [Authorize(Roles = nameof(UserRole.SupportAgent))]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> AddTimeEntry(Guid ticketId, [FromBody] CreateTimeEntryRequest request)
        {
            var response = await _timeEntriesService.AddTimeEntryAsync(ticketId, request, GetUserId());
            return ToActionResult(response);
        }
    }
}
