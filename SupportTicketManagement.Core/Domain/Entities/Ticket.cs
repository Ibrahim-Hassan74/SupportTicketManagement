using SupportTicketManagement.Core.Domain.IdentityEntities;
using SupportTicketManagement.Core.Enums;

namespace SupportTicketManagement.Core.Domain.Entities
{
    public class Ticket
    {
        public Guid Id { get; set; }

        public string Title { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public TicketStatus Status { get; set; } = TicketStatus.Open;

        public TicketPriority Priority { get; set; } = TicketPriority.Medium;

        public Guid CustomerId { get; set; }

        public Guid? AssignedAgentId { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset UpdatedAt { get; set; }

        public DateTimeOffset? ResolvedAt { get; set; }

        public DateTimeOffset? ClosedAt { get; set; }

        public byte[] RowVersion { get; set; } = null!;

        // Navigation properties
        public ApplicationUser Customer { get; set; } = null!;

        public ApplicationUser? AssignedAgent { get; set; }

        public ICollection<TicketComment> Comments { get; set; } = new List<TicketComment>();

        public ICollection<TicketActivity> Activities { get; set; } = new List<TicketActivity>();

        public ICollection<TimeEntry> TimeEntries { get; set; } = new List<TimeEntry>();
    }
}
