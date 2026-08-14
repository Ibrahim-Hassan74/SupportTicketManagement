using SupportTicketManagement.Core.DTO;
using SupportTicketManagement.Core.Enums;
using SupportTicketManagement.Core.Helper;
using SupportTicketManagement.Core.RepositoryContracts;
using SupportTicketManagement.Core.ServiceContracts;

namespace SupportTicketManagement.Core.Services
{
    public class ActivitiesService : IActivitiesService
    {
        private readonly IUnitOfWork _unitOfWork;

        public ActivitiesService(IUnitOfWork unitOfWork)
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

        public async Task<ApiResponse> GetActivitiesAsync(Guid ticketId, Guid userId, string role)
        {
            if (!await CanAccessTicketAsync(ticketId, userId, role))
                return ApiResponseFactory.NotFound("Ticket not found or you do not have access.");

            var activities = await _unitOfWork.TicketActivityRepository.GetFilteredAsync(
                a => a.TicketId == ticketId,
                a => a.User);

            var orderedActivities = activities.OrderBy(a => a.CreatedAt).ToList();

            var response = orderedActivities.Select(a => new ActivityResponse
            {
                Id = a.Id,
                TicketId = a.TicketId,
                UserId = a.UserId,
                UserName = a.User?.DisplayName ?? "Unknown",
                Type = a.Type.ToString(),
                Description = a.Description,
                OldValue = a.OldValue,
                NewValue = a.NewValue,
                CreatedAt = a.CreatedAt
            }).ToList();

            return ApiResponseFactory.Success("Activities retrieved successfully.", response);
        }
    }
}
