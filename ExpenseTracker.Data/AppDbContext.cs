using Microsoft.EntityFrameworkCore;
using ExpenseTracker.Data.Model;

namespace ExpenseTracker
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<CategoryParents> CategoryParents { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Category>()
                .Property(c => c.MccCodes)
                .HasDefaultValue("[]");

            modelBuilder.Entity<CategoryParents>()
                .HasKey(cp => new { cp.CategoryId, cp.ParentCategoryId });

            modelBuilder.Entity<CategoryParents>()
                .HasOne(cp => cp.Category)
                .WithMany()
                .HasForeignKey(cp => cp.CategoryId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CategoryParents>()
                .HasOne(cp => cp.ParentCategory)
                .WithMany()
                .HasForeignKey(cp => cp.ParentCategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}