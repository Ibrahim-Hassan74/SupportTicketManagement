using System.ComponentModel.DataAnnotations;

namespace SupportTicketManagement.Core.DTO
{
    public class AssignTicketRequest
    {
        [Required]
        public Guid AgentId { get; set; }
    }
}
