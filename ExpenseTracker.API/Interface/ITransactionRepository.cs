using ExpenseTracker.Data.Model;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ExpenseTracker.API
{
    public interface ITransactionRepository : IGenericRepository<Transaction>
    {
        Task<List<Transaction>> GetTransactionsByUserAndDateAsync(Guid userId, DateTime fromDate, DateTime toDate);
        Task<List<Transaction>> GetTransactionsByCategoriesAsync(Guid categoryId); // Legacy method
        Task<List<Transaction>> GetTransactionsByCategoriesAsync(Guid categoryId, int pageNumber, int pageSize); // New paginated method
        Task UpdateCategoryForTransactionsAsync(Guid categoryId, List<Guid> transactionIds);
        Task AddRangeAsync(IEnumerable<Transaction> transactions);
        Task<List<Guid>> GetValidExpenseTransactionIdsAsync(List<Guid> transactionIds);
        Task AddTransactionCategoryAsync(TransactionCategory transactionCategory);
    }
}
