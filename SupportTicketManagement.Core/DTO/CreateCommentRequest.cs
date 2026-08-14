using System.ComponentModel.DataAnnotations;

namespace SupportTicketManagement.Core.DTO
{
    public class CreateCommentRequest
    {
        [Required]
        [StringLength(2000)]
        public string Content { get; set; } = string.Empty;
    }
}
