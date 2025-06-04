using System.ComponentModel.DataAnnotations;

namespace ExpenseTracker.API
{
    public class CategoryDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public int[] MccCodes { get; set; }
        public bool IsBuiltIn { get; set; }
        public Guid? UserId { get; set; }
    }

    public class CreateCategoryDto
    {
        [Required]
        public string Name { get; set; }
        public List<Guid> ParentCategoryIds { get; set; }
    }
    public class UpdateCategoryDto
    {
        [Required]
        public string Name { get; set; }
        public List<Guid> BaseCategoryIds { get; set; } = new List<Guid>();
    }

    public class TransactionDto
    {
        public Guid Id { get; set; }
        public string Description { get; set; }
        public decimal Amount { get; set; }
        public DateTime Date { get; set; }
        public Guid CategoryId { get; set; }
        public string CategoryName { get; set; }
        public Guid UserId { get; set; }
        public string TransactionType { get; set; }
        public int? MccCode { get; set; }
        public bool IsManuallyCategorized { get; set; }
        public List<TransactionCategoryDto> TransactionCategories { get; set; }
    }

    public class TransactionCategoryDto
    {
        public Guid CategoryId { get; set; }
        public string CategoryName { get; set; }
        public bool IsBaseCategory { get; set; }
    }

    public class CreateTransactionDto
    {
        [Required]
        public string Description { get; set; }
        public decimal Amount { get; set; }
        public DateTime Date { get; set; }
        public Guid CategoryId { get; set; }
        public Guid UserId { get; set; }
        public string TransactionType { get; set; }
        public int? MccCode { get; set; }
        public bool IsManuallyCategorized { get; set; }
    }

    public class UpdateTransactionDto
    {
        public Guid Id { get; set; }
        [Required]
        public string Description { get; set; }
        public decimal Amount { get; set; }
        public DateTime Date { get; set; }
        public Guid CategoryId { get; set; }
        public string TransactionType { get; set; }
        public int? MccCode { get; set; }
        public bool IsManuallyCategorized { get; set; }
    }

    public class UpdateTransactionCategoryRequest
    {
        public List<Guid> TransactionIds { get; set; }
        public Guid CategoryId { get; set; }
    }
    public class UpdateUserCredentialsRequest
    {
        [Required(ErrorMessage = "Поточний пароль обов'язковий")]
        public string CurrentPassword { get; set; }

        [StringLength(25, MinimumLength = 4, ErrorMessage = "Новий логін має бути від 4 до 25 символів")]
        public string? NewLogin { get; set; }

        [StringLength(25, MinimumLength = 8, ErrorMessage = "Новий пароль має бути від 8 до 25 символів")]
        public string? NewPassword { get; set; }

        [Compare("NewPassword", ErrorMessage = "Паролі не співпадають")]
        public string? ConfirmNewPassword { get; set; }
    }
}
