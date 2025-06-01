using ExpenseTracker.Data.Model;

namespace ExpenseTracker.API
{
    public interface ICategoryRepository : IGenericRepository<Category>
    {
        Task<Category> GetDefaultCategoryAsync();
        Task AddRelationshipAsync(Guid customCategoryId, Guid baseCategoryId);
        Task RemoveRelationshipsAsync(Guid customCategoryId);
        Task<List<Category>> GetAllAsync(Guid? userId);
        Task<List<CategoryRelationship>> GetRelationshipsByBaseCategoryIdAsync(Guid baseCategoryId);
        Task<List<Transaction>> GetTransactionsByCategoryIdAsync(Guid categoryId, Guid userId);
    }
}