using System.Text.Json.Serialization;

namespace SupportTicketManagement.Core.Enums
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum UserRole
    {
        Admin,
        SupportAgent,
        Customer
    }
}
