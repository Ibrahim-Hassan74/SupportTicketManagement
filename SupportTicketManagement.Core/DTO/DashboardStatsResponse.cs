namespace SupportTicketManagement.Core.DTO
{
    public class DashboardStatsResponse
    {
        public int TotalTickets { get; set; }
        public int OpenTickets { get; set; }
        public int InProgressTickets { get; set; }
        public int ResolvedTickets { get; set; }
        public int ClosedTickets { get; set; }
        public int CriticalOpenTickets { get; set; }
        public double AvgResolutionTimeHours { get; set; }
    }
}
