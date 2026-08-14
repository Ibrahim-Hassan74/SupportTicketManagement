using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SupportTicketManagement.Core.DTO;
using SupportTicketManagement.Core.ServiceContracts;
using System.Security.Claims;

namespace SupportTicketManagement.API.Controllers
{
    /// <summary>
    /// Controller responsible for retrieving the activity timeline (audit trail) for a specific ticket.
    /// </summary>
    [ApiVersion(1.0)]
    [Route("api/v{version:apiVersion}/tickets/{ticketId:guid}/activities")]
    [Authorize]
    public class TicketActivitiesController : CustomControllerBase
    {
        private readonly IActivitiesService _activitiesService;

        public TicketActivitiesController(IActivitiesService activitiesService)
        {
            _activitiesService = activitiesService;
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
        /// Retrieves the chronological history of activities for a specific ticket.
        /// Users can only view the activity timeline for tickets they have access to.
        /// </summary>
        /// <param name="ticketId">The unique identifier of the ticket.</param>
        /// <returns>A list of activities representing the audit trail of the ticket.</returns>
        [HttpGet]
        [ProducesResponseType(typeof(List<ActivityResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetActivities(Guid ticketId)
        {
            var response = await _activitiesService.GetActivitiesAsync(ticketId, GetUserId(), GetUserRole());
            
            if (!response.Success)
                return ToActionResult(response);

            var responseData = response as ApiResponseWithData<List<ActivityResponse>>;
            return Ok(responseData?.Data);
        }
    }
}
