using SupportTicketManagement.Core.DTO;

namespace SupportTicketManagement.Core.ServiceContracts
{
    public interface IDashboardService
    {
        Task<ApiResponse> GetStatsAsync(string role);
        Task<ApiResponse> GetAgentWorkloadAsync(string role);
        Task<ApiResponse> GetTicketTrendsAsync(int days, string role);
    }
}
