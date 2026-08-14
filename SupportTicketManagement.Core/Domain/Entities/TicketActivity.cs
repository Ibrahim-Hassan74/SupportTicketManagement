using SupportTicketManagement.Core.Domain.IdentityEntities;
using SupportTicketManagement.Core.Enums;

namespace SupportTicketManagement.Core.Domain.Entities
{
    public class TicketActivity
    {
        public Guid Id { get; set; }

        public Guid TicketId { get; set; }

        public Guid UserId { get; set; }

        public ActivityType Type { get; set; }

        /// <summary>
        /// Human-readable summary of the activity (e.g., "Status changed from Open to InProgress").
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Previous value before the change (e.g., "Open"). Null for creation activities.
        /// </summary>
        public string? OldValue { get; set; }

        /// <summary>
        /// New value after the change (e.g., "InProgress"). Null for creation activities.
        /// </summary>
        public string? NewValue { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        // Navigation properties
        public Ticket Ticket { get; set; } = null!;

        public ApplicationUser User { get; set; } = null!;
    }
}
