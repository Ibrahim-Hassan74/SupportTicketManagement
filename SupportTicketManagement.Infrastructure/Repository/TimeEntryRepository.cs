using SupportTicketManagement.Core.Domain.Entities;
using SupportTicketManagement.Core.RepositoryContracts;
using SupportTicketManagement.Infrastructure.Data;

namespace SupportTicketManagement.Infrastructure.Repository
{
    public class TimeEntryRepository : GenericRepository<TimeEntry>, ITimeEntryRepository
    {
        public TimeEntryRepository(ApplicationDbContext context) : base(context)
        {
        }
    }
}
