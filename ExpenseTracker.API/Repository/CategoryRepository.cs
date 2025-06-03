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
            var customCategory = await _context.Categories.FindAsync(customCategoryId);
            if (customCategory == null)
            {
                throw new InvalidOperationException($"Custom category {customCategoryId} not found.");
            }

            var baseCategory = await _context.Categories.FindAsync(baseCategoryId);
            if (baseCategory == null || !baseCategory.IsBuiltIn)
            {
                throw new InvalidOperationException($"Base category {baseCategoryId} is invalid.");
            }

            var relationship = new CategoryRelationship
            {
                CustomCategoryId = customCategoryId,
                BaseCategoryId = baseCategoryId
            };
            await _context.CategoryRelationships.AddAsync(relationship);

            var customMccCodes = customCategory.MccCodesArray.ToList();
            var baseMccCodes = baseCategory.MccCodesArray.ToList();
            customMccCodes.AddRange(baseMccCodes.Except(customMccCodes));
            customCategory.MccCodesArray = customMccCodes.ToArray();
            _context.Categories.Update(customCategory);

            await _context.SaveChangesAsync();

            var baseTransactions = await _context.TransactionCategories
                .Where(tc => tc.CategoryId == baseCategoryId && tc.Transaction.UserId == customCategory.UserId)
                .Select(tc => tc.Transaction)
                .Distinct()
                .ToListAsync();

            if (baseTransactions.Any())
            {
                var transactionCategories = baseTransactions.Select(t => new TransactionCategory
                {
                    TransactionId = t.Id,
                    CategoryId = customCategoryId,
                    IsBaseCategory = false
                }).ToList();

                _context.TransactionCategories.AddRange(transactionCategories);
                await _context.SaveChangesAsync();
            }
        }

        public async Task RemoveRelationshipsAsync(Guid customCategoryId)
        {
            var customCategory = await _context.Categories.FindAsync(customCategoryId);
            if (customCategory == null)
            {
                return;
            }

            var relationships = await _context.CategoryRelationships
                .Where(cr => cr.CustomCategoryId == customCategoryId)
                .ToListAsync();
            _context.CategoryRelationships.RemoveRange(relationships);

            var transactionCategories = await _context.TransactionCategories
                .Where(tc => tc.CategoryId == customCategoryId)
                .ToListAsync();
            var transactionIds = transactionCategories.Select(tc => tc.TransactionId).ToList();
            _context.TransactionCategories.RemoveRange(transactionCategories);

            var defaultCategory = await GetDefaultCategoryAsync();
            if (defaultCategory != null && transactionIds.Any())
            {
                var defaultTransactionCategories = transactionIds.Select(tId => new TransactionCategory
                {
                    TransactionId = tId,
                    CategoryId = defaultCategory.Id,
                    IsBaseCategory = true
                }).ToList();
                _context.TransactionCategories.AddRange(defaultTransactionCategories);
            }

            customCategory.MccCodesArray = Array.Empty<int>();
            _context.Categories.Update(customCategory);

            await _context.SaveChangesAsync();
        }

        public async Task<List<Category>> GetAllAsync(Guid? userId)
        {
            return await _context.Categories
                .Where(c => c.IsBuiltIn || c.UserId == userId)
                .ToListAsync();
        }

        public async Task<List<Guid>> GetTransactionIdsByCategoryIdAsync(Guid categoryId, Guid userId)
        {
            return await _context.TransactionCategories
                .Where(tc => tc.CategoryId == categoryId && tc.Transaction.UserId == userId)
                .Select(tc => tc.TransactionId)
                .Distinct()
                .ToListAsync();
        }
        public async Task<List<Guid>> GetBaseCategoryIdsAsync(Guid customCategoryId)
        {
            return await _context.CategoryRelationships
                .Where(cr => cr.CustomCategoryId == customCategoryId)
                .Select(cr => cr.BaseCategoryId)
                .ToListAsync();
        }
        public async Task RemoveRelationshipAsync(Guid customCategoryId, Guid baseCategoryId)
        {
            var relationship = await _context.CategoryRelationships
                .FirstOrDefaultAsync(cr => cr.CustomCategoryId == customCategoryId && cr.BaseCategoryId == baseCategoryId);

            if (relationship != null)
            {
                _context.CategoryRelationships.Remove(relationship);
                await _context.SaveChangesAsync();
            }
        }
    }
}