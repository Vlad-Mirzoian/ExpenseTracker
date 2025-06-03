using ExpenseTracker.Data.Model;

namespace ExpenseTracker.API
{
    public interface ICategoryRepository : IGenericRepository<Category>
    {
        Task<Category> GetDefaultCategoryAsync();
        Task AddRelationshipAsync(Guid customCategoryId, Guid baseCategoryId);
        Task RemoveRelationshipsAsync(Guid customCategoryId);
        Task<List<Category>> GetAllAsync(Guid? userId);
        Task<List<Guid>> GetTransactionIdsByCategoryIdAsync(Guid categoryId, Guid userId);
        Task<List<Guid>> GetBaseCategoryIdsAsync(Guid customCategoryId);
        Task RemoveRelationshipAsync(Guid customCategoryId, Guid baseCategoryId);

    }
}