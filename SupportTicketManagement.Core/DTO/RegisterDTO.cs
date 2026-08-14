using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace SupportTicketManagement.Core.DTO
{
    public class RegisterDTO
    {
        [Required(ErrorMessage = "{0} can't be blank")]
        [Display(Name = "User Name")]
        public string? UserName { get; set; }
        [Required(ErrorMessage = "{0} can't be blank")]
        [EmailAddress(ErrorMessage = "{0} should be in a proper email address format")]
        public string? Email { get; set; }
        [Required(ErrorMessage = "{0} can't be blank")]
        [RegularExpression("^[0-9]*$", ErrorMessage = "{0} should contains numbers only")]
        [DefaultValue("01012345678")]
        public string? Phone { get; set; }
        [Required(ErrorMessage = "{0} can't be blank")]
        [DataType(DataType.Password)]
        [MinLength(8, ErrorMessage = "{0} should be at least 8 characters")]
        public string? Password { get; set; }
        [Required(ErrorMessage = "{0} can't be blank")]
        [Display(Name = "Confirm Password")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Passwords do not match")]
        public string? ConfirmPassword { get; set; }
    }
}
