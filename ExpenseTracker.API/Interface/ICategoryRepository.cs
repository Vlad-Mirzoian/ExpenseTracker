using ExpenseTracker.Data.Model;

namespace ExpenseTracker.API
{
    public interface ICategoryRepository : IGenericRepository<Category>
    {
        Task<Category> GetDefaultCategoryAsync();
        Task AddParentRelationshipAsync(Guid categoryId, Guid parentCategoryId);
        Task RemoveParentRelationshipsAsync(Guid categoryId);
    }
}