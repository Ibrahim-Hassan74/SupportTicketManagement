namespace SupportTicketManagement.Core.RepositoryContracts
{
    public interface IUnitOfWork
    {
        ITicketActivityRepository TicketActivityRepository { get; }
        ITicketsRepository TicketsRepository { get; }
        ITimeEntryRepository TimeEntryRepository { get; }
        Task<int> CompleteAsync();
    }
}
