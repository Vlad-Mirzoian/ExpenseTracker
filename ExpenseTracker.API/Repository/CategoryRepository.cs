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

        public async Task AddParentRelationshipAsync(Guid categoryId, Guid parentCategoryId)
        {
            var relationship = new CategoryParents
            {
                CategoryId = categoryId,
                ParentCategoryId = parentCategoryId
            };
            await _context.CategoryParents.AddAsync(relationship);
            await _context.SaveChangesAsync();
        }

        public async Task RemoveParentRelationshipsAsync(Guid categoryId)
        {
            var relationships = await _context.CategoryParents
                .Where(cp => cp.CategoryId == categoryId)
                .ToListAsync();
            _context.CategoryParents.RemoveRange(relationships);
            await _context.SaveChangesAsync();
        }
    }
}