using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.API
{
    public class TransactionCategorizationService
    {
        private readonly ICategoryRepository _categoryRepository;
        private readonly ILogger<TransactionCategorizationService> _logger;

        public TransactionCategorizationService(
            ICategoryRepository categoryRepository,
            ILogger<TransactionCategorizationService> logger)
        {
            _categoryRepository = categoryRepository ?? throw new ArgumentNullException(nameof(categoryRepository));
            _logger = logger;
        }

        public async Task<Guid> CategorizeTransactionAsync(int? mccCode)
        {
            _logger.LogDebug("Categorizing transaction with MCC {MccCode}", mccCode);

            if (!mccCode.HasValue || mccCode == 0)
            {
                _logger.LogDebug("MCC is null or 0, using default category");
                return (await _categoryRepository.GetDefaultCategoryAsync())?.Id ?? throw new InvalidOperationException("Default category not found.");
            }

            var categories = await _categoryRepository.GetAllAsync(null); // Built-in only
            var matchingCategory = categories.FirstOrDefault(c =>
                !string.IsNullOrEmpty(c.MccCodes) && c.MccCodesArray.Contains(mccCode.Value));

            if (matchingCategory != null)
            {
                _logger.LogDebug("Found matching category {CategoryId} for MCC {MccCode}", matchingCategory.Id, mccCode);
                return matchingCategory.Id;
            }

            _logger.LogDebug("No matching category for MCC {MccCode}, using default", mccCode);
            return (await _categoryRepository.GetDefaultCategoryAsync())?.Id ?? throw new InvalidOperationException("Default category not found.");
        }
    }
}