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
                return await GetOtherCategoryIdAsync();
            }

            var categories = await _categoryRepository.GetAllAsync();
            var matchingCategory = categories.FirstOrDefault(c => c.MccCodesArray.Contains(mccCode.Value));

            if (matchingCategory != null)
            {
                _logger.LogDebug("Found matching category {CategoryId} for MCC {MccCode}", matchingCategory.Id, mccCode);
                return matchingCategory.Id;
            }

            _logger.LogDebug("No matching category for MCC {MccCode}, using default", mccCode);
            return await GetOtherCategoryIdAsync();
        }

        private async Task<Guid> GetOtherCategoryIdAsync()
        {
            var otherCategory = await _categoryRepository.GetByNameAsync("Інше");
            if (otherCategory == null)
            {
                _logger.LogError("Category 'Інше' not found");
                throw new InvalidOperationException("Category 'Інше' not found.");
            }
            return otherCategory.Id;
        }
    }
}