using SupportTicketManagement.Core.Enums;
using System.ComponentModel.DataAnnotations;

namespace SupportTicketManagement.Core.DTO
{
    public class UpdateTicketStatusRequest
    {
        [Required]
        public TicketStatus Status { get; set; }
    }
}
