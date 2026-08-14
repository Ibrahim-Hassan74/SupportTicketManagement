using SupportTicketManagement.Core.RepositoryContracts;
using SupportTicketManagement.Infrastructure.Data;

namespace SupportTicketManagement.Infrastructure.Repository
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _context;
        public ITicketActivityRepository TicketActivityRepository { get; }

        public ITicketsRepository TicketsRepository { get; }

        public ITimeEntryRepository TimeEntryRepository { get; }

        public UnitOfWork(ApplicationDbContext context)
        {
            _context = context;
            TicketActivityRepository = new TicketActivityRepository(_context);
            TicketsRepository = new TicketsRepository(_context);
            TimeEntryRepository = new TimeEntryRepository(_context);
        }


        public async Task<int> CompleteAsync()
        {
            return await _context.SaveChangesAsync();
        }
    }
}
