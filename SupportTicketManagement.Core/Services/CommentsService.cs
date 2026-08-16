using SupportTicketManagement.Core.Domain.Entities;
using SupportTicketManagement.Core.DTO;
using SupportTicketManagement.Core.Enums;
using SupportTicketManagement.Core.Helper;
using SupportTicketManagement.Core.RepositoryContracts;
using SupportTicketManagement.Core.ServiceContracts;
using Microsoft.Extensions.Logging;

namespace SupportTicketManagement.Core.Services
{
    public class CommentsService : ICommentsService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<CommentsService> _logger;

        public CommentsService(IUnitOfWork unitOfWork, ILogger<CommentsService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        private async Task<bool> CanAccessTicketAsync(Guid ticketId, Guid userId, string role)
        {
            var ticket = await _unitOfWork.TicketsRepository.GetByIdAsync(ticketId);
            if (ticket == null) return false;

            if (role == UserRole.Admin.ToString())
                return true;

            if (role == UserRole.SupportAgent.ToString())
                return ticket.AssignedAgentId == userId;

            if (role == UserRole.Customer.ToString())
                return ticket.CustomerId == userId;

            return false;
        }

        public async Task<ApiResponse> GetCommentsAsync(Guid ticketId, Guid userId, string role)
        {
            if (!await CanAccessTicketAsync(ticketId, userId, role))
            {
                _logger.LogWarning("User {UserId} ({Role}) denied access to get comments for ticket {TicketId}.", userId, role, ticketId);
                return ApiResponseFactory.NotFound("Ticket not found or you do not have access.");
            }

            var comments = await _unitOfWork.TicketCommentRepository.GetFilteredAsync(
                c => c.TicketId == ticketId,
                c => c.User);

            var orderedComments = comments.OrderBy(c => c.CreatedAt).ToList();

            var response = orderedComments.Select(c => new CommentResponse
            {
                Id = c.Id,
                TicketId = c.TicketId,
                UserId = c.UserId,
                UserName = c.User?.DisplayName ?? "Unknown",
                Content = c.Content,
                CreatedAt = c.CreatedAt
            }).ToList();

            return ApiResponseFactory.Success("Comments retrieved successfully.", response);
        }

        public async Task<ApiResponse> AddCommentAsync(Guid ticketId, CreateCommentRequest request, Guid userId, string role)
        {
            ValidationHelper.ModelValidation(request);

            if (!await CanAccessTicketAsync(ticketId, userId, role))
            {
                _logger.LogWarning("User {UserId} ({Role}) denied access to add comment to ticket {TicketId}.", userId, role, ticketId);
                return ApiResponseFactory.NotFound("Ticket not found or you do not have access.");
            }

            var comment = new TicketComment
            {
                TicketId = ticketId,
                UserId = userId,
                Content = request.Content,
                CreatedAt = DateTimeOffset.UtcNow
            };

            await _unitOfWork.TicketCommentRepository.AddAsync(comment);

            var activity = new TicketActivity
            {
                TicketId = ticketId,
                UserId = userId,
                Type = ActivityType.CommentAdded,
                Description = "A new comment was added.",
                CreatedAt = DateTimeOffset.UtcNow
            };
            await _unitOfWork.TicketActivityRepository.AddAsync(activity);

            await _unitOfWork.CompleteAsync();

            _logger.LogInformation("User {UserId} added a comment to ticket {TicketId}.", userId, ticketId);
            return ApiResponseFactory.Success("Comment added successfully.");
        }
    }
}
