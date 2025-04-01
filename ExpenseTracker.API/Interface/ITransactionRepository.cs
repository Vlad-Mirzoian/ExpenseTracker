using ExpenseTracker.API.Repository;

namespace ExpenseTracker.API.Interface
{
    public interface ITransactionRepository: IGenericRepository<Transaction>
    {
        Task<List<Transaction>> GetTransactionsByUserAndDateAsync(Guid userId, DateTime transactionDateRange);
        Task<List<Transaction>> GetTransactionsByCategoriesAsync(Guid categoryId);
        Task UpdateCategoryForTransactions(Guid categoryId, List<Guid> transactionIds);
    }
}
