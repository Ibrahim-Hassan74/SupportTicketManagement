using SupportTicketManagement.Core.Enums;
using System.ComponentModel.DataAnnotations;

namespace SupportTicketManagement.Core.DTO
{
    public class CreateTicketRequest
    {
        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(4000)]
        public string Description { get; set; } = string.Empty;

        [Required]
        public TicketPriority Priority { get; set; } = TicketPriority.Medium;
    }
}
