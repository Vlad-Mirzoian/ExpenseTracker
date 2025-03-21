using ExpenseTracker.Data;
using ExpenseTracker.Data.Model;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class TransactionService
{
    private readonly AppDbContext _context;

    public TransactionService(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddTransactionAsync(Transaction transaction)
    {
        // Приводим дату к UTC, если она не в формате UTC
        if (transaction.Date.Kind != DateTimeKind.Utc)
        {
            transaction.Date = transaction.Date.ToUniversalTime();
        }

        // Получаем категорию по ID или создаём категорию "Інше", если её не существует
        if (transaction.CategoryId.HasValue)
        {
            var category = await _context.Categories
                .FirstOrDefaultAsync(c => c.Id == transaction.CategoryId.Value);

            if (category == null)
            {
                // Если категория не найдена, присваиваем категорию "Інше"
                var defaultCategory = await _context.Categories
                    .FirstOrDefaultAsync(c => c.Name == "Інше");

                if (defaultCategory == null)
                {
                    // Если категории "Інше" нет в базе, создаём её
                    defaultCategory = new Category
                    {
                        Id = Guid.NewGuid(),
                        Name = "Інше",
                        MccCodeList = null // Примерный код для категории "Інше"
                    };

                    _context.Categories.Add(defaultCategory);
                    await _context.SaveChangesAsync();
                }

                // Присваиваем категорию "Інше"
                transaction.CategoryId = defaultCategory.Id;
            }
        }

        // Добавляем транзакцию в базу данных
        _context.Transactions.Add(transaction);
        await _context.SaveChangesAsync();
    }
}
