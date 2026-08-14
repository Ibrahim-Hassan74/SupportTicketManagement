using Microsoft.EntityFrameworkCore;
using SupportTicketManagement.Core.Domain.Entities;
using SupportTicketManagement.Core.DTO;
using SupportTicketManagement.Core.Enums;
using SupportTicketManagement.Core.RepositoryContracts;
using SupportTicketManagement.Infrastructure.Data;
using System.Linq.Expressions;

namespace SupportTicketManagement.Infrastructure.Repository
{
    public class TicketsRepository : GenericRepository<Ticket>, ITicketsRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly DbSet<Ticket> _db;

        public TicketsRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
            _db = context.Set<Ticket>();
        }

        public async Task<int> CountFilteredTicketsAsync(Expression<Func<Ticket, bool>>? predicate)
        {
            IQueryable<Ticket> query = _db;
            if (predicate != null)
            {
                query = query.Where(predicate);
            }
            return await query.CountAsync();
        }

        public async Task<IEnumerable<Ticket>> GetFilteredTicketsAsync(
            Expression<Func<Ticket, bool>>? predicate, 
            Func<IQueryable<Ticket>, IOrderedQueryable<Ticket>>? orderBy, 
            int pageNumber, 
            int pageSize, 
            params Expression<Func<Ticket, object>>[] includes)
        {
            IQueryable<Ticket> query = _db;

            if (includes != null)
            {
                foreach (var include in includes)
                {
                    query = query.Include(include);
                }
            }

            if (predicate != null)
            {
                query = query.Where(predicate);
            }

            if (orderBy != null)
            {
                query = orderBy(query);
            }

            return await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<DashboardStatsResponse> GetDashboardStatsAsync()
        {
            var stats = new DashboardStatsResponse();

            stats.TotalTickets = await _db.CountAsync();
            stats.OpenTickets = await _db.CountAsync(t => t.Status == TicketStatus.Open);
            stats.InProgressTickets = await _db.CountAsync(t => t.Status == TicketStatus.InProgress);
            stats.ResolvedTickets = await _db.CountAsync(t => t.Status == TicketStatus.Resolved);
            stats.ClosedTickets = await _db.CountAsync(t => t.Status == TicketStatus.Closed);
            stats.CriticalOpenTickets = await _db.CountAsync(t => t.Status == TicketStatus.Open && t.Priority == TicketPriority.Critical);

            var resolvedTicketsList = await _db
                .Where(t => t.Status == TicketStatus.Resolved || t.Status == TicketStatus.Closed)
                .Where(t => t.ResolvedAt != null)
                .Select(t => new { t.CreatedAt, t.ResolvedAt })
                .ToListAsync();

            if (resolvedTicketsList.Any())
            {
                var totalHours = resolvedTicketsList.Sum(t => (t.ResolvedAt!.Value - t.CreatedAt).TotalHours);
                stats.AvgResolutionTimeHours = Math.Round(totalHours / resolvedTicketsList.Count, 2);
            }

            return stats;
        }

        public async Task<IEnumerable<AgentWorkloadResponse>> GetAgentWorkloadAsync()
        {
            var workload = await _context.Users
                .Where(u => u.AssignedTickets.Any())
                .Select(u => new AgentWorkloadResponse
                {
                    AgentId = u.Id,
                    AgentName = u.DisplayName,
                    OpenTickets = u.AssignedTickets.Count(t => t.Status == TicketStatus.Open),
                    InProgressTickets = u.AssignedTickets.Count(t => t.Status == TicketStatus.InProgress),
                    TotalTimeMinutes = u.TimeEntries.Sum(te => te.DurationMinutes)
                })
                .ToListAsync();

            return workload;
        }

        public async Task<IEnumerable<TicketTrendResponse>> GetTicketTrendsAsync(int days)
        {
            var cutoff = DateTimeOffset.UtcNow.AddDays(-days);

            var tickets = await _db
                .Where(t => t.CreatedAt >= cutoff || (t.ClosedAt != null && t.ClosedAt >= cutoff))
                .Select(t => new { t.CreatedAt, t.ClosedAt })
                .ToListAsync();

            var trends = new List<TicketTrendResponse>();

            for (int i = 0; i < days; i++)
            {
                var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-i));
                trends.Add(new TicketTrendResponse
                {
                    Date = date,
                    OpenCount = tickets.Count(t => DateOnly.FromDateTime(t.CreatedAt.Date) == date),
                    ClosedCount = tickets.Count(t => t.ClosedAt != null && DateOnly.FromDateTime(t.ClosedAt.Value.Date) == date)
                });
            }

            return trends.OrderBy(t => t.Date).ToList();
        }
    }
}
