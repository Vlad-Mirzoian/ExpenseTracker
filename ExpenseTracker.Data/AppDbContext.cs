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
        public DbSet<CategoryRelationship> CategoryRelationships { get; set; }
        public DbSet<TransactionCategory> TransactionCategories { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Category>()
                .Property(c => c.MccCodes)
                .HasDefaultValue("[]");

            modelBuilder.Entity<CategoryRelationship>()
                .HasKey(cr => new { cr.CustomCategoryId, cr.BaseCategoryId });

            modelBuilder.Entity<CategoryRelationship>()
                .HasOne(cr => cr.CustomCategory)
                .WithMany()
                .HasForeignKey(cr => cr.CustomCategoryId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CategoryRelationship>()
                .HasOne(cr => cr.BaseCategory)
                .WithMany()
                .HasForeignKey(cr => cr.BaseCategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TransactionCategory>()
                .HasKey(tc => new { tc.TransactionId, tc.CategoryId });

            modelBuilder.Entity<TransactionCategory>()
                .HasOne(tc => tc.Transaction)
                .WithMany(t => t.TransactionCategories)
                .HasForeignKey(tc => tc.TransactionId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<TransactionCategory>()
                .HasOne(tc => tc.Category)
                .WithMany()
                .HasForeignKey(tc => tc.CategoryId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Transaction>()
                .HasOne(t => t.Category)
                .WithMany()
                .HasForeignKey(t => t.CategoryId);
        }
    }
}