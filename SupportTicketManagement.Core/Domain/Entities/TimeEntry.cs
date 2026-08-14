using SupportTicketManagement.Core.Domain.IdentityEntities;

namespace SupportTicketManagement.Core.Domain.Entities
{
    public class TimeEntry
    {
        public Guid Id { get; set; }

        public Guid TicketId { get; set; }

        public Guid AgentId { get; set; }

        public DateOnly WorkDate { get; set; }

        public int DurationMinutes { get; set; }

        public string Description { get; set; } = string.Empty;

        public DateTimeOffset CreatedAt { get; set; }

        // Navigation properties
        public Ticket Ticket { get; set; } = null!;

        public ApplicationUser Agent { get; set; } = null!;
    }
}
