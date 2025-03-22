using ExpenseTracker.Data.Model;
using Microsoft.EntityFrameworkCore;

public class AppDbContext : DbContext
{
    public DbSet<User> Users { get; set; }
    public DbSet<Transaction> Transactions { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<MerchantAlias> MerchantAliases { get; set; } // Добавлено

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Category>().HasData(
            new Category { Id = Guid.Parse("c1b15d7e-0b6f-4d19-9d8c-b0c8722277d0"), Name = "Кофейні", MccCodes = "5814" },
            new Category { Id = Guid.Parse("b7d45c1b-19b4-4770-bcf4-8c2f5e4d3424"), Name = "Ресторани", MccCodes = "5812" },
            new Category { Id = Guid.Parse("a25a42f4-88b3-4006-b0c3-2c7a15a358e7"), Name = "Супермаркети", MccCodes = "5411,5499" },
            new Category { Id = Guid.Parse("ad42b743-ef9a-43e5-b71f-97a742ae1a85"), Name = "Магазини одягу", MccCodes = "5651" },
            new Category { Id = Guid.Parse("61e1f6c7-7b85-47d1-bb9a-d78f911e8cd3"), Name = "АЗС", MccCodes = "5541" },
            new Category { Id = Guid.Parse("db60d0b9-89e6-4694-9295-56b688254a2f"), Name = "Банки", MccCodes = "6011" },
            new Category { Id = Guid.Parse("1a12c08c-f9eb-4f29-8480-ef05137e0cf5"), Name = "Аптеки", MccCodes = "5912" },
            new Category { Id = Guid.Parse("d177f3d7-d5d2-4d97-bd9e-45f54e2e268f"), Name = "Готелі", MccCodes = "7011" },
            new Category { Id = Guid.Parse("B50EE9F0-3480-40D8-BB48-E15BF9E2FC03"), Name = "Доставка", MccCodes = "5811,8999" },
            new Category { Id = Guid.Parse("2AFD336A-03B8-4F17-8CFE-2C0E93CB4C49"), Name = "Знаття/Відправка", MccCodes = "4829" },
            new Category { Id = Guid.Parse("CF668835-71A2-4868-87B8-3938AC9DABF9"), Name = "Комуналка", MccCodes = "4900" },
            new Category { Id = Guid.Parse("86CDEB09-AE28-4A8F-9006-24CE4C44419F"), Name = "Надходження", MccCodes = "6012" },
            new Category { Id = Guid.Parse("74d258ea-bf82-4934-bb9a-8a343d9da1ea"), Name = "Кінотеатри", MccCodes = "7832" }
        );
    }

}
