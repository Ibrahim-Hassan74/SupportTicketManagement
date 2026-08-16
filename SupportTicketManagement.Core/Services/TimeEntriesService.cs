using SupportTicketManagement.Core.Domain.Entities;
using SupportTicketManagement.Core.DTO;
using SupportTicketManagement.Core.Enums;
using SupportTicketManagement.Core.Helper;
using SupportTicketManagement.Core.RepositoryContracts;
using SupportTicketManagement.Core.ServiceContracts;
using Microsoft.Extensions.Logging;

namespace SupportTicketManagement.Core.Services
{
    public class TimeEntriesService : ITimeEntriesService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<TimeEntriesService> _logger;

        public TimeEntriesService(IUnitOfWork unitOfWork, ILogger<TimeEntriesService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<ApiResponse> GetTimeEntriesAsync(Guid ticketId, Guid userId, string role)
        {
            var ticket = await _unitOfWork.TicketsRepository.GetByIdAsync(ticketId);
            if (ticket == null)
                return ApiResponseFactory.NotFound("Ticket not found.");

            // Only Admins and assigned Agents can view time entries
            if (role == UserRole.Customer.ToString())
            {
                _logger.LogWarning("Customer {UserId} denied access to get time entries for ticket {TicketId}.", userId, ticketId);
                return ApiResponseFactory.NotFound("Ticket not found."); // IDOR protection
            }

            if (role == UserRole.SupportAgent.ToString() && ticket.AssignedAgentId != userId)
            {
                _logger.LogWarning("Agent {AgentId} denied access to get time entries for ticket {TicketId}.", userId, ticketId);
                return ApiResponseFactory.NotFound("Ticket not found or not assigned to you.");
            }

            var timeEntries = await _unitOfWork.TimeEntryRepository.GetFilteredAsync(
                t => t.TicketId == ticketId,
                t => t.Agent);

            var orderedEntries = timeEntries.OrderBy(t => t.WorkDate).ThenBy(t => t.CreatedAt).ToList();

            var response = new TicketTimeEntriesResponse
            {
                TotalDurationMinutes = orderedEntries.Sum(t => t.DurationMinutes),
                Entries = orderedEntries.Select(t => new TimeEntryResponse
                {
                    Id = t.Id,
                    TicketId = t.TicketId,
                    AgentId = t.AgentId,
                    AgentName = t.Agent?.DisplayName ?? "Unknown",
                    WorkDate = t.WorkDate,
                    DurationMinutes = t.DurationMinutes,
                    Description = t.Description,
                    CreatedAt = t.CreatedAt
                }).ToList()
            };

            return ApiResponseFactory.Success("Time entries retrieved successfully.", response);
        }

        public async Task<ApiResponse> AddTimeEntryAsync(Guid ticketId, CreateTimeEntryRequest request, Guid agentId)
        {
            ValidationHelper.ModelValidation(request);

            var ticket = await _unitOfWork.TicketsRepository.GetByIdAsync(ticketId);
            if (ticket == null || ticket.AssignedAgentId != agentId)
            {
                _logger.LogWarning("Agent {AgentId} denied access to add time entry to ticket {TicketId}.", agentId, ticketId);
                return ApiResponseFactory.NotFound("Ticket not found or not assigned to you.");
            }

            if (request.WorkDate.ToDateTime(TimeOnly.MinValue) > DateTime.UtcNow)
                return ApiResponseFactory.BadRequest("Work date cannot be in the future.");

            var timeEntry = new TimeEntry
            {
                TicketId = ticketId,
                AgentId = agentId,
                WorkDate = request.WorkDate,
                DurationMinutes = request.DurationMinutes,
                Description = request.Description,
                CreatedAt = DateTimeOffset.UtcNow
            };

            await _unitOfWork.TimeEntryRepository.AddAsync(timeEntry);

            var activity = new TicketActivity
            {
                TicketId = ticketId,
                UserId = agentId,
                Type = ActivityType.TimeLogged,
                Description = $"Logged {request.DurationMinutes} minutes of work.",
                CreatedAt = DateTimeOffset.UtcNow
            };
            await _unitOfWork.TicketActivityRepository.AddAsync(activity);

            await _unitOfWork.CompleteAsync();

            _logger.LogInformation("Agent {AgentId} logged {Duration} minutes on ticket {TicketId}.", agentId, request.DurationMinutes, ticketId);
            return ApiResponseFactory.Success("Time entry added successfully.");
        }
    }
}
