using SupportTicketManagement.Core.Enums;

namespace SupportTicketManagement.Core.DTO
{
    public class UserQueryRequest
    {
        public UserRole? Role { get; set; }

        public string? Search { get; set; }

        public int Page { get; set; } = 1;

        public int PageSize { get; set; } = 10;
    }
}
