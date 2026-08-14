using System.ComponentModel.DataAnnotations;

namespace SupportTicketManagement.Core.DTO
{
    public class UpdateUserRequest
    {
        [Required(ErrorMessage = "{0} can't be blank")]
        [MaxLength(100, ErrorMessage = "{0} can't be longer than {1} characters")]
        public string DisplayName { get; set; } = string.Empty;

        public bool IsActive { get; set; }
    }
}
