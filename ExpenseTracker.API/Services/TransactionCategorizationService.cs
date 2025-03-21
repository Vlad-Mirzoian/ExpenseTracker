using Microsoft.EntityFrameworkCore;

public class TransactionCategorizationService
{
    private readonly AppDbContext _context;
    private readonly Guid DefaultCategoryId = Guid.Parse("66666666-6666-6666-6666-666666666666"); // "Інше"

    public TransactionCategorizationService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Guid> CategorizeTransactionAsync(int mccCode)
    {
        // Проверка на существующие категории, основанные на MCC
        var category = await _context.Categories
            .FirstOrDefaultAsync(c => c.MccCodeList.Contains(mccCode));


        if (category != null)
            return category.Id;

        // Если категории с таким MCC нет, возвращаем категорию "Інше"
        return DefaultCategoryId;
    }
}
