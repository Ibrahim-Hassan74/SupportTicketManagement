using Microsoft.AspNetCore.Identity;
using SupportTicketManagement.Core.Domain.Entities;

namespace SupportTicketManagement.Core.Domain.IdentityEntities
{
    public class ApplicationUser : IdentityUser<Guid>
    {
        public string DisplayName { get; set; } = string.Empty;

        public string? RefreshToken { get; set; }

        public DateTimeOffset RefreshTokenExpirationDateTime { get; set; }

        public bool IsDeleted { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        // Navigation properties

        /// <summary>
        /// Tickets created by this user (when role is Customer).
        /// </summary>
        public ICollection<Ticket> CreatedTickets { get; set; } = new List<Ticket>();

        /// <summary>
        /// Tickets assigned to this user (when role is SupportAgent).
        /// </summary>
        public ICollection<Ticket> AssignedTickets { get; set; } = new List<Ticket>();

        public ICollection<TicketComment> Comments { get; set; } = new List<TicketComment>();

        public ICollection<TimeEntry> TimeEntries { get; set; } = new List<TimeEntry>();
    }
}
