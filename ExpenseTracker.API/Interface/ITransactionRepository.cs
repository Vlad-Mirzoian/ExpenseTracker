using ExpenseTracker.Data.Model;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ExpenseTracker.API
{
    public interface ITransactionRepository : IGenericRepository<Transaction>
    {
        Task<List<Transaction>> GetTransactionsByUserAndDateAsync(Guid userId, DateTime fromDate, DateTime toDate);
        Task<List<Transaction>> GetTransactionsByCategoriesAsync(Guid categoryId, Guid userId);
        Task<List<Transaction>> GetTransactionsByCategoriesAsync(Guid categoryId, Guid userId, int pageNumber, int pageSize);
        Task RemoveTransactionCategoriesAsync(Guid categoryId, List<Guid> transactionIds);
        Task ReassignOrphanedTransactionsAsync(List<Guid> transactionIds, Guid defaultCategoryId);
        Task UpdateCategoryForTransactionsAsync(Guid categoryId, List<Guid> transactionIds);
        Task AddRangeAsync(IEnumerable<Transaction> transactions);
        Task<List<Guid>> GetValidExpenseTransactionIdsAsync(List<Guid> transactionIds);
        Task AddTransactionCategoriesAsync(List<TransactionCategory> transactionCategories);
    }
}
