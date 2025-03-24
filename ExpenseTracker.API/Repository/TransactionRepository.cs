using ExpenseTracker.API.Interface;
using ExpenseTracker.Data.Model;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.API.Repository
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
        public async Task<List<Transaction>> GetTransactionsByUserAndDateAsync(Guid userId, DateTime transactionDateRange)
        {
            return await _context.Transactions
            .Where(tx => tx.UserId == userId && tx.Date >= transactionDateRange)
            .ToListAsync();
        }
    }
}
