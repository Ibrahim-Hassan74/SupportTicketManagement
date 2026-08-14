using SupportTicketManagement.Core.Domain.Entities;
using SupportTicketManagement.Core.RepositoryContracts;
using SupportTicketManagement.Infrastructure.Data;

namespace SupportTicketManagement.Infrastructure.Repository
{
    public class TicketActivityRepository : GenericRepository<TicketActivity>, ITicketActivityRepository
    {
        public TicketActivityRepository(ApplicationDbContext context) : base(context)
        {
        }
    }
}
