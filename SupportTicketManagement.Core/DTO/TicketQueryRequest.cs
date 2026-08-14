using SupportTicketManagement.Core.Enums;
using System.ComponentModel.DataAnnotations;

namespace SupportTicketManagement.Core.DTO
{
    public class TicketQueryRequest
    {
        public TicketStatus? Status { get; set; }
        public TicketPriority? Priority { get; set; }
        public string? Search { get; set; }
        public SortByOptions? SortBy { get; set; }
        public SortOrderOptions? SortOrder { get; set; }
        
        [Range(1, int.MaxValue)]
        public int Page { get; set; } = 1;
        
        [Range(1, 100)]
        public int PageSize { get; set; } = 10;
    }
}
