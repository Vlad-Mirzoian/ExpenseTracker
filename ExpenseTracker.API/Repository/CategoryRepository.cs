using ExpenseTracker.Data.Model;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.API
{
    public class CategoryRepository : GenericRepository<Category>, ICategoryRepository
    {
        private readonly AppDbContext _context;

        public CategoryRepository(AppDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<Category> GetDefaultCategoryAsync()
        {
            return await _context.Categories
                .FirstOrDefaultAsync(c => c.Name == "Інше" && c.IsBuiltIn);
        }

        public async Task AddRelationshipAsync(Guid customCategoryId, Guid baseCategoryId)
        {
            var relationship = new CategoryRelationship
            {
                CustomCategoryId = customCategoryId,
                BaseCategoryId = baseCategoryId
            };
            await _context.CategoryRelationships.AddAsync(relationship);
            await _context.SaveChangesAsync();

            var customCategory = await _context.Categories.FindAsync(customCategoryId);
            var baseTransactions = await _context.Transactions
                .Where(t => t.CategoryId == baseCategoryId && t.UserId == customCategory.UserId)
                .ToListAsync();

            foreach (var transaction in baseTransactions)
            {
                var tc = new TransactionCategory
                {
                    TransactionId = transaction.Id,
                    CategoryId = customCategoryId,
                    IsBaseCategory = false
                };
                await _context.TransactionCategories.AddAsync(tc);
            }
            await _context.SaveChangesAsync();
        }

        public async Task RemoveRelationshipsAsync(Guid customCategoryId)
        {
            var relationships = await _context.CategoryRelationships
                .Where(cr => cr.CustomCategoryId == customCategoryId)
                .ToListAsync();
            _context.CategoryRelationships.RemoveRange(relationships);

            var transactionCategories = await _context.TransactionCategories
                .Where(tc => tc.CategoryId == customCategoryId)
                .ToListAsync();
            _context.TransactionCategories.RemoveRange(transactionCategories);

            await _context.SaveChangesAsync();
        }

        public async Task<List<Category>> GetAllAsync(Guid? userId)
        {
            return await _context.Categories
                .Where(c => c.IsBuiltIn || c.UserId == userId)
                .ToListAsync();
        }

        public async Task<List<CategoryRelationship>> GetRelationshipsByBaseCategoryIdAsync(Guid baseCategoryId)
        {
            return await _context.CategoryRelationships
                .Include(cr => cr.CustomCategory)
                .Where(cr => cr.BaseCategoryId == baseCategoryId)
                .ToListAsync();
        }

        public async Task<List<Transaction>> GetTransactionsByCategoryIdAsync(Guid categoryId, Guid userId)
        {
            return await _context.TransactionCategories
                .Where(tc => tc.CategoryId == categoryId && tc.Transaction.UserId == userId)
                .Select(tc => tc.Transaction)
                .ToListAsync();
        }
    }
}