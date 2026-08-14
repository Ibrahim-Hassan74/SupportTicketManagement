using System.ComponentModel.DataAnnotations;

namespace SupportTicketManagement.Core.DTO
{
    public class CreateTimeEntryRequest
    {
        [Required]
        public DateOnly WorkDate { get; set; }

        [Required]
        [Range(1, 1440, ErrorMessage = "Duration must be between 1 and 1440 minutes (24 hours).")]
        public int DurationMinutes { get; set; }

        [Required]
        [StringLength(500)]
        public string Description { get; set; } = string.Empty;
    }
}
