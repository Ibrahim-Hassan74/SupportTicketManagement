using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SupportTicketManagement.Core.DTO;
using SupportTicketManagement.Core.ServiceContracts;
using System.Security.Claims;

namespace SupportTicketManagement.API.Controllers
{
    /// <summary>
    /// Controller responsible for managing comments on a specific support ticket.
    /// Provides endpoints for retrieving and adding comments.
    /// </summary>
    [ApiVersion(1.0)]
    [Route("api/v{version:apiVersion}/tickets/{ticketId:guid}/comments")]
    [Authorize]
    public class TicketCommentsController : CustomControllerBase
    {
        private readonly ICommentsService _commentsService;

        public TicketCommentsController(ICommentsService commentsService)
        {
            _commentsService = commentsService;
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
        /// Retrieves a chronological list of comments for a specific ticket.
        /// Users can only view comments for tickets they have access to.
        /// </summary>
        /// <param name="ticketId">The unique identifier of the ticket.</param>
        /// <returns>A list of comments associated with the ticket.</returns>
        [HttpGet]
        [ProducesResponseType(typeof(List<CommentResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetComments(Guid ticketId)
        {
            var response = await _commentsService.GetCommentsAsync(ticketId, GetUserId(), GetUserRole());
            
            if (!response.Success)
                return ToActionResult(response);

            var responseData = response as ApiResponseWithData<List<CommentResponse>>;
            return Ok(responseData?.Data);
        }

        /// <summary>
        /// Adds a new comment to a specific ticket.
        /// </summary>
        /// <param name="ticketId">The unique identifier of the ticket.</param>
        /// <param name="request">The content of the new comment.</param>
        /// <returns>A success response if the comment is added.</returns>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> AddComment(Guid ticketId, [FromBody] CreateCommentRequest request)
        {
            var response = await _commentsService.AddCommentAsync(ticketId, request, GetUserId(), GetUserRole());
            return ToActionResult(response);
        }
    }
}
