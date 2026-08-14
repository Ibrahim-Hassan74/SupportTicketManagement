namespace SupportTicketManagement.Core.RepositoryContracts
{
    public interface IUnitOfWork
    {
        Task<int> CompleteAsync();
    }
}
