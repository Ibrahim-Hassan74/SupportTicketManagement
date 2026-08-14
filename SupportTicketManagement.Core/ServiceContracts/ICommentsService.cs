using SupportTicketManagement.Core.DTO;

namespace SupportTicketManagement.Core.ServiceContracts
{
    public interface ICommentsService
    {
        Task<ApiResponse> GetCommentsAsync(Guid ticketId, Guid userId, string role);
        Task<ApiResponse> AddCommentAsync(Guid ticketId, CreateCommentRequest request, Guid userId, string role);
    }
}
