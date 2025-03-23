using ExpenseTracker.API.Repository;

namespace ExpenseTracker.API.Interface
{
    public interface ICategoryRepository: IGenericRepository<Category>
    {
        Category? GetByMccCode(int mccCode);
    }
}
