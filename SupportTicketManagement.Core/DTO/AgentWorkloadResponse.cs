namespace SupportTicketManagement.Core.DTO
{
    public class AgentWorkloadResponse
    {
        public Guid AgentId { get; set; }
        public string AgentName { get; set; } = string.Empty;
        public int OpenTickets { get; set; }
        public int InProgressTickets { get; set; }
        public int TotalTimeMinutes { get; set; }
    }
}
