using SupportTicketManagement.Core.Domain.Entities;
using SupportTicketManagement.Core.DTO;
using SupportTicketManagement.Core.Enums;
using SupportTicketManagement.Core.Helper;
using SupportTicketManagement.Core.RepositoryContracts;
using SupportTicketManagement.Core.ServiceContracts;

namespace SupportTicketManagement.Core.Services
{
    public class CommentsService : ICommentsService
    {
        private readonly IUnitOfWork _unitOfWork;

        public CommentsService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
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
                return ApiResponseFactory.NotFound("Ticket not found or you do not have access.");

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
                return ApiResponseFactory.NotFound("Ticket not found or you do not have access.");

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

            return ApiResponseFactory.Success("Comment added successfully.");
        }
    }
}
