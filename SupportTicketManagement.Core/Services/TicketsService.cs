using Microsoft.AspNetCore.Identity;
using SupportTicketManagement.Core.Domain.Entities;
using SupportTicketManagement.Core.Domain.IdentityEntities;
using SupportTicketManagement.Core.DTO;
using SupportTicketManagement.Core.Enums;
using SupportTicketManagement.Core.Helper;
using SupportTicketManagement.Core.RepositoryContracts;
using SupportTicketManagement.Core.ServiceContracts;
using System.Linq.Expressions;

namespace SupportTicketManagement.Core.Services
{
    public class TicketsService : ITicketsService
    {
        private readonly ITicketsRepository _ticketsRepository;
        private readonly ITicketActivityRepository _ticketActivityRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<ApplicationUser> _userManager;

        public TicketsService(
            IUnitOfWork unitOfWork, 
            UserManager<ApplicationUser> userManager)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _ticketsRepository = unitOfWork.TicketsRepository;
            _ticketActivityRepository = unitOfWork.TicketActivityRepository;
        }

        public async Task<ApiResponse> AssignAgentAsync(Guid id, AssignTicketRequest request, Guid adminId)
        {
            ValidationHelper.ModelValidation(request);

            var ticket = await _ticketsRepository.GetByIdAsync(id);
            if (ticket == null)
                return ApiResponseFactory.NotFound("Ticket not found.");

            var agent = await _userManager.FindByIdAsync(request.AgentId.ToString());
            if (agent == null)
                return ApiResponseFactory.NotFound("Agent not found.");

            var isAgent = await _userManager.IsInRoleAsync(agent, UserRole.SupportAgent.ToString());
            if (!isAgent)
                return ApiResponseFactory.BadRequest("The specified user is not a Support Agent.");

            ticket.AssignedAgentId = request.AgentId;
            ticket.UpdatedAt = DateTimeOffset.UtcNow;

            var activity = new TicketActivity
            {
                TicketId = ticket.Id,
                UserId = adminId,
                Type = ActivityType.AgentAssigned,
                Description = $"Ticket assigned to agent {agent.DisplayName}",
                OldValue = null,
                NewValue = request.AgentId.ToString(),
                CreatedAt = DateTimeOffset.UtcNow
            };
            await _ticketActivityRepository.AddAsync(activity);

            await _ticketsRepository.UpdateAsync(ticket);
            await _unitOfWork.CompleteAsync();

            return ApiResponseFactory.Success("Agent assigned successfully.");
        }

        public async Task<ApiResponse> CreateTicketAsync(CreateTicketRequest request, Guid customerId)
        {
            ValidationHelper.ModelValidation(request);

            var customer = await _userManager.FindByIdAsync(customerId.ToString());
            if (customer == null)
                return ApiResponseFactory.NotFound("Customer not found.");

            var ticket = new Ticket
            {
                Title = request.Title,
                Description = request.Description,
                Priority = request.Priority,
                Status = TicketStatus.Open,
                CustomerId = customerId,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            var createdTicket = await _ticketsRepository.AddAsync(ticket);

            var activity = new TicketActivity
            {
                TicketId = createdTicket.Id,
                UserId = customerId,
                Type = ActivityType.Created,
                Description = "Ticket created.",
                CreatedAt = DateTimeOffset.UtcNow
            };
            await _ticketActivityRepository.AddAsync(activity);

            await _unitOfWork.CompleteAsync();

            var response = MapToResponse(createdTicket);
            response.CustomerName = customer.DisplayName;

            return ApiResponseFactory.Success("Ticket created successfully.", response);
        }

        public async Task<ApiResponse> GetTicketByIdForAdminAsync(Guid id)
        {
            return await GetTicketByIdInternalAsync(id, null);
        }

        public async Task<ApiResponse> GetTicketByIdForAgentAsync(Guid id, Guid agentId)
        {
            return await GetTicketByIdInternalAsync(id, t => t.AssignedAgentId == agentId);
        }

        public async Task<ApiResponse> GetTicketByIdForCustomerAsync(Guid id, Guid customerId)
        {
            return await GetTicketByIdInternalAsync(id, t => t.CustomerId == customerId);
        }

        public async Task<ApiResponse> GetTicketsForAdminAsync(TicketQueryRequest request)
        {
            return await GetTicketsInternalAsync(request, null);
        }

        public async Task<ApiResponse> GetTicketsForAgentAsync(TicketQueryRequest request, Guid agentId)
        {
            return await GetTicketsInternalAsync(request, t => t.AssignedAgentId == agentId);
        }

        public async Task<ApiResponse> GetTicketsForCustomerAsync(TicketQueryRequest request, Guid customerId)
        {
            return await GetTicketsInternalAsync(request, t => t.CustomerId == customerId);
        }

        public async Task<ApiResponse> UpdateTicketAsync(Guid id, UpdateTicketRequest request, Guid adminId)
        {
            ValidationHelper.ModelValidation(request);

            var ticket = await _ticketsRepository.GetByIdAsync(id);
            if (ticket == null)
                return ApiResponseFactory.NotFound("Ticket not found.");

            ticket.Title = request.Title;
            ticket.Description = request.Description;
            ticket.UpdatedAt = DateTimeOffset.UtcNow;

            await _ticketsRepository.UpdateAsync(ticket);
            await _unitOfWork.CompleteAsync();

            return ApiResponseFactory.Success("Ticket updated successfully.");
        }

        public async Task<ApiResponse> UpdateTicketPriorityAsync(Guid id, UpdateTicketPriorityRequest request, Guid adminId)
        {
            ValidationHelper.ModelValidation(request);

            var ticket = await _ticketsRepository.GetByIdAsync(id);
            if (ticket == null)
                return ApiResponseFactory.NotFound("Ticket not found.");

            var oldPriority = ticket.Priority;
            ticket.Priority = request.Priority;
            ticket.UpdatedAt = DateTimeOffset.UtcNow;

            if (oldPriority != request.Priority)
            {
                var activity = new TicketActivity
                {
                    TicketId = ticket.Id,
                    UserId = adminId,
                    Type = ActivityType.PriorityChanged,
                    Description = $"Priority changed from {oldPriority} to {request.Priority}",
                    OldValue = oldPriority.ToString(),
                    NewValue = request.Priority.ToString(),
                    CreatedAt = DateTimeOffset.UtcNow
                };
                await _ticketActivityRepository.AddAsync(activity);
            }

            await _ticketsRepository.UpdateAsync(ticket);
            await _unitOfWork.CompleteAsync();

            return ApiResponseFactory.Success("Ticket priority updated successfully.");
        }

        public async Task<ApiResponse> UpdateTicketStatusByAdminAsync(Guid id, UpdateTicketStatusRequest request, Guid adminId)
        {
            return await UpdateTicketStatusInternalAsync(id, request.Status, true, true, adminId);
        }

        public async Task<ApiResponse> UpdateTicketStatusByAgentAsync(Guid id, UpdateTicketStatusRequest request, Guid agentId)
        {
            var ticket = await _ticketsRepository.GetByIdAsync(id);
            if (ticket == null || ticket.AssignedAgentId != agentId)
                return ApiResponseFactory.NotFound("Ticket not found or not assigned to you.");

            bool canClose = false;
            bool canResolve = true; // Agents can resolve

            return await UpdateTicketStatusInternalAsync(id, request.Status, canResolve, canClose, agentId, ticket);
        }

        public async Task<ApiResponse> UpdateTicketStatusByCustomerAsync(Guid id, UpdateTicketStatusRequest request, Guid customerId)
        {
            var ticket = await _ticketsRepository.GetByIdAsync(id);
            if (ticket == null || ticket.CustomerId != customerId)
                return ApiResponseFactory.NotFound("Ticket not found or does not belong to you.");

            bool canClose = true; // Customers can close
            bool canResolve = false; // Customers cannot mark as resolved (only reopen or close)

            if (request.Status == TicketStatus.InProgress || request.Status == TicketStatus.Resolved)
                return ApiResponseFactory.BadRequest("Customers cannot set status to InProgress or Resolved.");

            return await UpdateTicketStatusInternalAsync(id, request.Status, canResolve, canClose, customerId, ticket);
        }

        // ── Internal Helpers ──────────────────────────────────────────

        private async Task<ApiResponse> GetTicketByIdInternalAsync(Guid id, Expression<Func<Ticket, bool>>? rolePredicate)
        {
            var ticket = await _ticketsRepository.GetByIdAsync(id, t => t.Customer, t => t.AssignedAgent!);
            if (ticket == null)
                return ApiResponseFactory.NotFound("Ticket not found.");

            if (rolePredicate != null)
            {
                var compiledPredicate = rolePredicate.Compile();
                if (!compiledPredicate(ticket))
                    return ApiResponseFactory.NotFound("Ticket not found."); // IDOR protection: return 404 instead of 403 to hide existence
            }

            return ApiResponseFactory.Success("Ticket retrieved successfully.", MapToResponse(ticket));
        }
        private async Task<ApiResponse> GetTicketsInternalAsync(TicketQueryRequest request, Expression<Func<Ticket, bool>>? rolePredicate)
        {
            Expression<Func<Ticket, bool>>? searchPredicate = null;

            if (request.Status.HasValue || request.Priority.HasValue || !string.IsNullOrWhiteSpace(request.Search))
            {
                searchPredicate = t =>
                    (!request.Status.HasValue || t.Status == request.Status.Value) &&
                    (!request.Priority.HasValue || t.Priority == request.Priority.Value) &&
                    (string.IsNullOrWhiteSpace(request.Search) || t.Title.Contains(request.Search));
            }

            // Combine role predicate with search predicate
            Expression<Func<Ticket, bool>>? finalPredicate = null;
            if (rolePredicate != null && searchPredicate != null)
            {
                var parameter = Expression.Parameter(typeof(Ticket)); // t => ..
                // AndAlso mean AND (rolePredicate AND searchPredicate)
                var combined = Expression.AndAlso(
                    Expression.Invoke(rolePredicate, parameter), // t => rolePredicate(t)
                    Expression.Invoke(searchPredicate, parameter)); // t => searchPredicate(t)
                finalPredicate = Expression.Lambda<Func<Ticket, bool>>(combined, parameter);
                // finalPredicate =
                // t =>
                //    t.AssignedAgentId == agentId &&
                //  t.Status == TicketStatus.Open;
            }
            else
            {
                finalPredicate = rolePredicate ?? searchPredicate;
            }

            Func<IQueryable<Ticket>, IOrderedQueryable<Ticket>>? orderBy = null;

            if (request.SortBy.HasValue)
            {
                bool isDesc = request.SortOrder == SortOrderOptions.Descending;
                orderBy = request.SortBy switch
                {
                    SortByOptions.CreatedAt => q => isDesc ? q.OrderByDescending(t => t.CreatedAt) : q.OrderBy(t => t.CreatedAt),
                    SortByOptions.Priority => q => isDesc ? q.OrderByDescending(t => t.Priority) : q.OrderBy(t => t.Priority),
                    SortByOptions.Status => q => isDesc ? q.OrderByDescending(t => t.Status) : q.OrderBy(t => t.Status),
                    SortByOptions.UpdatedAt => q => isDesc ? q.OrderByDescending(t => t.UpdatedAt) : q.OrderBy(t => t.UpdatedAt),
                    _ => q => q.OrderByDescending(t => t.CreatedAt)
                };
            }
            else
            {
                orderBy = q => q.OrderByDescending(t => t.CreatedAt); // Default
            }

            var tickets = await _ticketsRepository.GetFilteredTicketsAsync(
                finalPredicate,
                orderBy,
                request.Page,
                request.PageSize,
                t => t.Customer, t => t.AssignedAgent!);

            var totalCount = await _ticketsRepository.CountFilteredTicketsAsync(finalPredicate);

            var responses = tickets.Select(MapToResponse).ToList();

            var paginatedResult = new PaginatedResponse<TicketResponse>
            {
                Items = responses,
                Page = request.Page,
                PageSize = request.PageSize,
                TotalCount = totalCount
            };

            return ApiResponseFactory.Success("Tickets retrieved successfully.", paginatedResult);
        }

        private async Task<ApiResponse> UpdateTicketStatusInternalAsync(Guid id, TicketStatus newStatus, bool canResolve, bool canClose, Guid userId, Ticket? existingTicket = null)
        {
            var ticket = existingTicket ?? await _ticketsRepository.GetByIdAsync(id);
            if (ticket == null)
                return ApiResponseFactory.NotFound("Ticket not found.");

            // Business Rules Validation
            if (newStatus == TicketStatus.Resolved && !canResolve)
                return ApiResponseFactory.BadRequest("You are not authorized to resolve tickets.");
            if (newStatus == TicketStatus.Closed && !canClose)
                return ApiResponseFactory.BadRequest("You are not authorized to close tickets.");

            // Basic state machine validation
            if (ticket.Status == TicketStatus.Open && newStatus == TicketStatus.Resolved)
                return ApiResponseFactory.BadRequest("Cannot resolve an open ticket. Must be In Progress first.");

            if (ticket.Status == TicketStatus.Closed && newStatus != TicketStatus.Open)
                return ApiResponseFactory.BadRequest("A closed ticket can only be reopened (set to Open).");

            var oldStatus = ticket.Status;
            ticket.Status = newStatus;
            ticket.UpdatedAt = DateTimeOffset.UtcNow;

            if (newStatus == TicketStatus.Resolved)
                ticket.ResolvedAt = DateTimeOffset.UtcNow;
            if (newStatus == TicketStatus.Closed)
                ticket.ClosedAt = DateTimeOffset.UtcNow;

            if (oldStatus != newStatus)
            {
                var activity = new TicketActivity
                {
                    TicketId = ticket.Id,
                    UserId = userId,
                    Type = newStatus == TicketStatus.Closed ? ActivityType.Closed : ActivityType.StatusChanged,
                    Description = $"Status changed from {oldStatus} to {newStatus}",
                    OldValue = oldStatus.ToString(),
                    NewValue = newStatus.ToString(),
                    CreatedAt = DateTimeOffset.UtcNow
                };
                await _ticketActivityRepository.AddAsync(activity);
            }

            await _ticketsRepository.UpdateAsync(ticket);
            await _unitOfWork.CompleteAsync();

            return ApiResponseFactory.Success("Ticket status updated successfully.");
        }

        private static TicketResponse MapToResponse(Ticket ticket)
        {
            return new TicketResponse
            {
                Id = ticket.Id,
                Title = ticket.Title,
                Description = ticket.Description,
                Priority = ticket.Priority.ToString(),
                Status = ticket.Status.ToString(),
                CustomerId = ticket.CustomerId,
                CustomerName = ticket.Customer?.DisplayName ?? string.Empty,
                AssignedAgentId = ticket.AssignedAgentId,
                AssignedAgentName = ticket.AssignedAgent?.DisplayName,
                CreatedAt = ticket.CreatedAt,
                UpdatedAt = ticket.UpdatedAt,
                ResolvedAt = ticket.ResolvedAt,
                ClosedAt = ticket.ClosedAt
            };
        }
    }
}
