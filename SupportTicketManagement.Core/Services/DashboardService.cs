using SupportTicketManagement.Core.DTO;
using SupportTicketManagement.Core.Enums;
using SupportTicketManagement.Core.Helper;
using SupportTicketManagement.Core.RepositoryContracts;
using SupportTicketManagement.Core.ServiceContracts;

namespace SupportTicketManagement.Core.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly IUnitOfWork _unitOfWork;

        public DashboardService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<ApiResponse> GetAgentWorkloadAsync(string role)
        {
            if (role != nameof(UserRole.Admin))
                return ApiResponseFactory.Forbidden("Only administrators can access dashboard metrics.");

            var workload = await _unitOfWork.TicketsRepository.GetAgentWorkloadAsync();
            return ApiResponseFactory.Success("Agent workload retrieved successfully.", workload);
        }

        public async Task<ApiResponse> GetStatsAsync(string role)
        {
            if (role != nameof(UserRole.Admin))
                return ApiResponseFactory.Forbidden("Only administrators can access dashboard metrics.");

            var stats = await _unitOfWork.TicketsRepository.GetDashboardStatsAsync();
            return ApiResponseFactory.Success("Dashboard stats retrieved successfully.", stats);
        }

        public async Task<ApiResponse> GetTicketTrendsAsync(int days, string role)
        {
            if (role != nameof(UserRole.Admin))
                return ApiResponseFactory.Forbidden("Only administrators can access dashboard metrics.");

            if (days <= 0 || days > 365)
                return ApiResponseFactory.BadRequest("Days parameter must be between 1 and 365.");

            var trends = await _unitOfWork.TicketsRepository.GetTicketTrendsAsync(days);
            return ApiResponseFactory.Success("Ticket trends retrieved successfully.", trends);
        }
    }
}
