namespace SupportTicketManagement.Core.DTO
{
    public class TicketTrendResponse
    {
        public DateOnly Date { get; set; }
        public int OpenCount { get; set; }
        public int ClosedCount { get; set; }
    }
}
