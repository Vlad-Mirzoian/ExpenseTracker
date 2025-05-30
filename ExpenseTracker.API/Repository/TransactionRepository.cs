using ExpenseTracker.Data.Model;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.API
{
    public class TransactionRepository : GenericRepository<Transaction>, ITransactionRepository
    {
        public TransactionRepository(AppDbContext context) : base(context) { }

        public async Task<List<Transaction>> GetTransactionsByCategoriesAsync(Guid categoryId)
        {
            return await _context.Transactions
                .Where(t => t.CategoryId == categoryId)
                .OrderByDescending(t => t.Date)
                .ToListAsync();
        }

        public async Task<List<Transaction>> GetTransactionsByUserAndDateAsync(Guid userId, DateTime fromDate, DateTime toDate)
        {
            if (fromDate > toDate)
                throw new ArgumentException("From date cannot be later than to date.", nameof(fromDate));

            return await _context.Transactions
                .Include(t => t.Category)
                .Where(tx => tx.UserId == userId && tx.Date >= fromDate && tx.Date <= toDate)
                .ToListAsync();
        }

        public async Task UpdateCategoryForTransactionsAsync(Guid categoryId, List<Guid> transactionIds)
        {
            if (transactionIds == null || !transactionIds.Any())
                throw new ArgumentException("Transaction IDs list cannot be empty.", nameof(transactionIds));

            if (categoryId == Guid.Empty)
                throw new ArgumentException("Category ID cannot be empty.", nameof(categoryId));

            var categoryExists = await _context.Categories.AnyAsync(c => c.Id == categoryId);
            if (!categoryExists)
                throw new InvalidOperationException($"Category with ID {categoryId} does not exist.");

            await _context.Transactions
                .Where(t => transactionIds.Contains(t.Id) && t.TransactionType == "Expense")
                .ExecuteUpdateAsync(s => s
                    .SetProperty(t => t.CategoryId, categoryId)
                    .SetProperty(t => t.IsManuallyCategorized, true));
        }

        public async Task AddRangeAsync(IEnumerable<Transaction> transactions)
        {
            if (transactions == null || !transactions.Any())
                throw new ArgumentException("Transactions list cannot be empty.", nameof(transactions));

            await _context.Transactions.AddRangeAsync(transactions);
            await _context.SaveChangesAsync();
        }

        public async Task<List<Guid>> GetValidExpenseTransactionIdsAsync(List<Guid> transactionIds)
        {
            if (transactionIds == null || !transactionIds.Any())
                return new List<Guid>();

            return await _context.Transactions
                .Where(t => transactionIds.Contains(t.Id) && t.TransactionType == "Expense")
                .Select(t => t.Id)
                .ToListAsync();
        }
    }
}