using ExpenseTracker.Data.Model;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.API
{
    public class TransactionRepository : GenericRepository<Transaction>, ITransactionRepository
    {
        private readonly AppDbContext _context;

        public TransactionRepository(AppDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<List<Transaction>> GetTransactionsByUserAndDateAsync(Guid userId, DateTime fromDate, DateTime toDate)
        {
            if (fromDate > toDate)
                throw new ArgumentException("From date cannot be later than to date.", nameof(fromDate));

            return await _context.Transactions
                .Include(t => t.Category)
                .Include(t => t.TransactionCategories)
                .ThenInclude(tc => tc.Category)
                .Where(tx => tx.UserId == userId && tx.Date >= fromDate && tx.Date <= toDate)
                .ToListAsync();
        }

        public async Task<List<Transaction>> GetTransactionsByCategoriesAsync(Guid categoryId)
        {
            var transactions = await _context.Transactions
                .Include(t => t.Category)
                .Include(t => t.TransactionCategories).ThenInclude(tc => tc.Category)
                .Where(t => t.TransactionCategories.Any(tc => tc.CategoryId == categoryId))
                .OrderByDescending(t => t.Date)
                .ToListAsync();
            return transactions;
        }

        public async Task<List<Transaction>> GetTransactionsByCategoriesAsync(Guid categoryId, int pageNumber, int pageSize)
        {
            var transactions = await _context.Transactions
                .Include(t => t.Category)
                .Include(t => t.TransactionCategories).ThenInclude(tc => tc.Category)
                .Where(t => t.TransactionCategories.Any(tc => tc.CategoryId == categoryId))
                .OrderByDescending(t => t.Date)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            return transactions;
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

            await _context.TransactionCategories
                .Where(tc => transactionIds.Contains(tc.TransactionId) && tc.IsBaseCategory)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(tc => tc.CategoryId, categoryId));

            var relationships = await _context.CategoryRelationships
                .Where(cr => cr.BaseCategoryId == categoryId)
                .ToListAsync();

            foreach (var transactionId in transactionIds)
            {
                foreach (var rel in relationships)
                {
                    var existing = await _context.TransactionCategories
                        .AnyAsync(tc => tc.TransactionId == transactionId && tc.CategoryId == rel.CustomCategoryId);
                    if (!existing)
                    {
                        await _context.TransactionCategories.AddAsync(new TransactionCategory
                        {
                            TransactionId = transactionId,
                            CategoryId = rel.CustomCategoryId,
                            IsBaseCategory = false
                        });
                    }
                }
            }
            await _context.SaveChangesAsync();
        }

        public async Task AddRangeAsync(IEnumerable<Transaction> transactions)
        {
            if (transactions == null || !transactions.Any())
                throw new ArgumentException("Transactions list cannot be empty.", nameof(transactions));

            await _context.Transactions.AddRangeAsync(transactions);
            foreach (var transaction in transactions)
            {
                await _context.TransactionCategories.AddAsync(new TransactionCategory
                {
                    TransactionId = transaction.Id,
                    CategoryId = transaction.CategoryId,
                    IsBaseCategory = true
                });
                var relationships = await _context.CategoryRelationships
                    .Where(cr => cr.BaseCategoryId == transaction.CategoryId && cr.CustomCategory.UserId == transaction.UserId)
                    .ToListAsync();
                foreach (var rel in relationships)
                {
                    await _context.TransactionCategories.AddAsync(new TransactionCategory
                    {
                        TransactionId = transaction.Id,
                        CategoryId = rel.CustomCategoryId,
                        IsBaseCategory = false
                    });
                }
            }
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

        public async Task AddTransactionCategoryAsync(TransactionCategory transactionCategory)
        {
            await _context.TransactionCategories.AddAsync(transactionCategory);
            await _context.SaveChangesAsync();
        }
    }
}