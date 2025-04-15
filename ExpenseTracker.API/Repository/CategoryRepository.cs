using ExpenseTracker.API.Interface;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace ExpenseTracker.API.Repository
{
    public class CategoryRepository : GenericRepository<Category>, ICategoryRepository
    {
        public CategoryRepository(AppDbContext context) : base(context) { }


        public Category? GetByMccCode(int mccCode)
        {
            return _context.Categories
                            .AsEnumerable()
                            .FirstOrDefault(c => c.MccCodesArray.Contains(mccCode));
        }

    }
}
