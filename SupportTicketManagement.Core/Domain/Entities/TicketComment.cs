using SupportTicketManagement.Core.Domain.IdentityEntities;

namespace SupportTicketManagement.Core.Domain.Entities
{
    public class TicketComment
    {
        public Guid Id { get; set; }

        public Guid TicketId { get; set; }

        public Guid UserId { get; set; }

        public string Content { get; set; } = string.Empty;

        public DateTimeOffset CreatedAt { get; set; }

        // Navigation properties
        public Ticket Ticket { get; set; } = null!;

        public ApplicationUser User { get; set; } = null!;
    }
}
