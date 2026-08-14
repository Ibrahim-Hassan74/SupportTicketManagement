using Microsoft.AspNetCore.Identity;

namespace SupportTicketManagement.Core.Domain.IdentityEntities
{
    public class ApplicationUser : IdentityUser<Guid>
    {
        public string DisplayName { get; set; } = string.Empty;
        public string? RefreshToken { get; set; }
        public bool IsDeleted { get; set; }
        public DateTimeOffset RefreshTokenExpirationDateTime { get; set; }
    }
}
