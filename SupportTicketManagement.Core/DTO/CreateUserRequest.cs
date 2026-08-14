using System.ComponentModel.DataAnnotations;

namespace SupportTicketManagement.Core.DTO
{
    public class CreateUserRequest
    {
        [Required(ErrorMessage = "{0} can't be blank")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "{0} can't be blank")]
        [MinLength(8, ErrorMessage = "{0} must be at least {1} characters long")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "{0} can't be blank")]
        [MaxLength(100, ErrorMessage = "{0} can't be longer than {1} characters"    )]
        public string DisplayName { get; set; } = string.Empty;

        [Required(ErrorMessage = "{0} can't be blank")]
        public string Role { get; set; } = string.Empty;
    }
}
