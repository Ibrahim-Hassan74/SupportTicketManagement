using SupportTicketManagement.Core.DTO;

namespace SupportTicketManagement.Core.ServiceContracts
{
    public interface ITimeEntriesService
    {
        Task<ApiResponse> GetTimeEntriesAsync(Guid ticketId, Guid userId, string role);
        Task<ApiResponse> AddTimeEntryAsync(Guid ticketId, CreateTimeEntryRequest request, Guid agentId);
    }
}
