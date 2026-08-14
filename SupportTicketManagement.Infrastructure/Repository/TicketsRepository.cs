using Microsoft.EntityFrameworkCore;
using SupportTicketManagement.Core.Domain.Entities;
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
    }
}
