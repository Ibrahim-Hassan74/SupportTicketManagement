namespace SupportTicketManagement.Core.DTO
{
    public class TicketTimeEntriesResponse
    {
        public int TotalDurationMinutes { get; set; }
        public List<TimeEntryResponse> Entries { get; set; } = new List<TimeEntryResponse>();
    }
}
