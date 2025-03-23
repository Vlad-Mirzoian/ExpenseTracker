using ExpenseTracker.API.Interface;
using Microsoft.EntityFrameworkCore;

public class TransactionCategorizationService
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly Guid DefaultCategoryId = Guid.Parse("66666666-6666-6666-6666-666666666666"); // "Інше"

    public TransactionCategorizationService(ICategoryRepository categoryRepository)
    {
        _categoryRepository = categoryRepository;
    }

    public async Task<Guid> CategorizeTransactionAsync(int mccCode)
    {
        var category = _categoryRepository.GetByMccCode(mccCode);

        if (category != null)
            return category.Id;

        return DefaultCategoryId;
    }
}
