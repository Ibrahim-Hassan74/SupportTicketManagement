using System.Text.Json.Serialization;

namespace SupportTicketManagement.Core.Enums
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum SortByOptions
    {
        CreatedAt,
        Priority,
        Status,
        UpdatedAt
    }
}
