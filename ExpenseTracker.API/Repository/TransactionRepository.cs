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

        public async Task<List<Transaction>> GetTransactionsByCategoriesAsync(Guid categoryId, Guid userId)
        {
            var transactions = await _context.Transactions
                .Include(t => t.Category)
                .Include(t => t.TransactionCategories)
                    .ThenInclude(tc => tc.Category)
                .Where(t => t.UserId == userId && t.TransactionCategories.Any(tc => tc.CategoryId == categoryId))
                .OrderByDescending(t => t.Date)
                .ToListAsync();
            return transactions;
        }

        public async Task<List<Transaction>> GetTransactionsByCategoriesAsync(Guid categoryId, Guid userId, int pageNumber, int pageSize)
        {
            var query = _context.Transactions
                .AsNoTracking()
                .Include(t => t.Category)
                .Include(t => t.TransactionCategories)
                    .ThenInclude(tc => tc.Category)
                .Where(t => t.UserId == userId && t.TransactionCategories.Any(tc => tc.CategoryId == categoryId))
                .OrderByDescending(t => t.Date);

            var totalCount = await query.CountAsync();
            var transactions = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            return transactions;
        }

        public async Task UpdateCategoryForTransactionsAsync(Guid categoryId, List<Guid> transactionIds)
        {
            if (transactionIds == null || !transactionIds.Any())
            {
                throw new ArgumentException("Transaction IDs list cannot be empty.", nameof(transactionIds));
            }

            if (categoryId == Guid.Empty)
            {
                throw new ArgumentException("Category ID cannot be empty.", nameof(categoryId));
            }

            var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var categoryExists = await _context.Categories.AnyAsync(c => c.Id == categoryId);
                if (!categoryExists)
                {
                    throw new InvalidOperationException($"Category with ID {categoryId} does not exist.");
                }

                await _context.TransactionCategories
                    .Where(tc => transactionIds.Contains(tc.TransactionId) && tc.IsBaseCategory)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(tc => tc.CategoryId, categoryId));

                await _context.Transactions
                    .Where(t => transactionIds.Contains(t.Id))
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(t => t.CategoryId, categoryId));

                var currentCustomCategories = await _context.TransactionCategories
                    .Where(tc => transactionIds.Contains(tc.TransactionId) && !tc.IsBaseCategory)
                    .Select(tc => new { tc.TransactionId, tc.CategoryId })
                    .ToListAsync();

                var validCustomCategoryIds = await _context.CategoryRelationships
                    .Where(cr => cr.BaseCategoryId == categoryId)
                    .Select(cr => cr.CustomCategoryId)
                    .ToListAsync();

                var customCategoriesToRemove = currentCustomCategories
                    .Where(tc => !validCustomCategoryIds.Contains(tc.CategoryId))
                    .ToList();

                if (customCategoriesToRemove.Any())
                {
                    await _context.TransactionCategories
                        .Where(tc => transactionIds.Contains(tc.TransactionId) &&
                                     !tc.IsBaseCategory &&
                                     !validCustomCategoryIds.Contains(tc.CategoryId))
                        .ExecuteDeleteAsync();
                }

                var newMappings = new List<TransactionCategory>();
                foreach (var transactionId in transactionIds)
                {
                    foreach (var customCategoryId in validCustomCategoryIds)
                    {
                        var existing = currentCustomCategories
                            .Any(tc => tc.TransactionId == transactionId && tc.CategoryId == customCategoryId);
                        if (!existing)
                        {
                            newMappings.Add(new TransactionCategory
                            {
                                TransactionId = transactionId,
                                CategoryId = customCategoryId,
                                IsBaseCategory = false
                            });
                        }
                    }
                }

                if (newMappings.Any())
                {
                    await _context.TransactionCategories.AddRangeAsync(newMappings);
                }
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task RemoveTransactionCategoriesAsync(Guid categoryId, List<Guid> transactionIds)
        {
            if (transactionIds == null || !transactionIds.Any())
                return;

            var transactionCategories = await _context.TransactionCategories
                .Where(tc => tc.CategoryId == categoryId && transactionIds.Contains(tc.TransactionId))
                .ToListAsync();

            if (transactionCategories.Any())
            {
                _context.TransactionCategories.RemoveRange(transactionCategories);
                await _context.SaveChangesAsync();
            }
        }

        public async Task ReassignOrphanedTransactionsAsync(List<Guid> transactionIds, Guid defaultCategoryId)
        {
            if (transactionIds == null || !transactionIds.Any())
                return;

            var categoryExists = await _context.Categories.AnyAsync(c => c.Id == defaultCategoryId);
            if (!categoryExists)
                throw new InvalidOperationException($"Default category with ID {defaultCategoryId} does not exist.");

            var transactionsToReassign = await _context.Transactions
                .Where(t => transactionIds.Contains(t.Id))
                .Where(t => !_context.TransactionCategories.Any(tc => tc.TransactionId == t.Id))
                .Select(t => t.Id)
                .ToListAsync();

            if (transactionsToReassign.Any())
            {
                var defaultTransactionCategories = transactionsToReassign.Select(tId => new TransactionCategory
                {
                    TransactionId = tId,
                    CategoryId = defaultCategoryId,
                    IsBaseCategory = true
                }).ToList();
                _context.TransactionCategories.AddRange(defaultTransactionCategories);
                await _context.SaveChangesAsync();
            }
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

        public async Task AddTransactionCategoriesAsync(List<TransactionCategory> transactionCategories)
        {
            if (transactionCategories != null || !transactionCategories.Any())
                return;

            await _context.TransactionCategories.AddRangeAsync(transactionCategories);
            await _context.SaveChangesAsync();
        }
    }
}