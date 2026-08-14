using SupportTicketManagement.Core.Domain.Entities;
using SupportTicketManagement.Core.RepositoryContracts;
using SupportTicketManagement.Infrastructure.Data;

namespace SupportTicketManagement.Infrastructure.Repository
{
    public class TicketCommentRepository : GenericRepository<TicketComment>, ITicketCommentRepository
    {
        public TicketCommentRepository(ApplicationDbContext context) : base(context)
        {
        }
    }
}
