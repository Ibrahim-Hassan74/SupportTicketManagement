using SupportTicketManagement.Core.DTO;

namespace SupportTicketManagement.Core.ServiceContracts
{
    public interface ITicketsService
    {
        Task<ApiResponse> GetTicketsForAdminAsync(TicketQueryRequest request);
        Task<ApiResponse> GetTicketsForAgentAsync(TicketQueryRequest request, Guid agentId);
        Task<ApiResponse> GetTicketsForCustomerAsync(TicketQueryRequest request, Guid customerId);

        Task<ApiResponse> GetTicketByIdForAdminAsync(Guid id);
        Task<ApiResponse> GetTicketByIdForAgentAsync(Guid id, Guid agentId);
        Task<ApiResponse> GetTicketByIdForCustomerAsync(Guid id, Guid customerId);

        Task<ApiResponse> CreateTicketAsync(CreateTicketRequest request, Guid customerId);
        Task<ApiResponse> UpdateTicketAsync(Guid id, UpdateTicketRequest request, Guid adminId);

        Task<ApiResponse> UpdateTicketStatusByAdminAsync(Guid id, UpdateTicketStatusRequest request, Guid adminId);
        Task<ApiResponse> UpdateTicketStatusByAgentAsync(Guid id, UpdateTicketStatusRequest request, Guid agentId);
        Task<ApiResponse> UpdateTicketStatusByCustomerAsync(Guid id, UpdateTicketStatusRequest request, Guid customerId);

        Task<ApiResponse> UpdateTicketPriorityAsync(Guid id, UpdateTicketPriorityRequest request, Guid adminId);
        Task<ApiResponse> AssignAgentAsync(Guid id, AssignTicketRequest request, Guid adminId);
    }
}
