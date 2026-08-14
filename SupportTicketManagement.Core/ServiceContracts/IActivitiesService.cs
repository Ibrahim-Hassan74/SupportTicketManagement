using SupportTicketManagement.Core.DTO;

namespace SupportTicketManagement.Core.ServiceContracts
{
    public interface IActivitiesService
    {
        Task<ApiResponse> GetActivitiesAsync(Guid ticketId, Guid userId, string role);
    }
}
