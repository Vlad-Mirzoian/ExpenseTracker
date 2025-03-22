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
        var category = _context.Categories
            .AsEnumerable() // Переключаемся на обработку в памяти (C#)
            .FirstOrDefault(c => c.MccCodeList.Contains(mccCode));

        if (category != null)
            return category.Id;

        return DefaultCategoryId;
    }
}
