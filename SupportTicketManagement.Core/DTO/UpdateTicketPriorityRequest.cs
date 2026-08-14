using SupportTicketManagement.Core.Enums;
using System.ComponentModel.DataAnnotations;

namespace SupportTicketManagement.Core.DTO
{
    public class UpdateTicketPriorityRequest
    {
        [Required]
        public TicketPriority Priority { get; set; }
    }
}
