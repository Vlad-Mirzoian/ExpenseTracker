using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;


namespace ExpenseTracker.API
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class TransactionController : ControllerBase
    {
        private readonly ITransactionRepository _transactionRepository;
        private readonly ICategoryRepository _categoryRepository;

        public TransactionController(
            ITransactionRepository transactionRepository,
            ICategoryRepository categoryRepository)
        {
            _transactionRepository = transactionRepository ?? throw new ArgumentNullException(nameof(transactionRepository));
            _categoryRepository = categoryRepository ?? throw new ArgumentNullException(nameof(categoryRepository));
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<TransactionDto>>> GetTransactions()
        {
            var currentUserId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var fromDate = DateTime.UtcNow.AddMonths(-12); // Last 12 months
            var toDate = DateTime.UtcNow;

            var transactions = await _transactionRepository.GetTransactionsByUserAndDateAsync(currentUserId, fromDate, toDate);
            var transactionDtos = transactions.Select(t => new TransactionDto
            {
                Id = t.Id,
                Description = t.Description,
                Amount = t.Amount,
                Date = t.Date,
                CategoryId = t.CategoryId,
                CategoryName = t.Category?.Name,
                UserId = t.UserId,
                TransactionType = t.TransactionType,
                MccCode = t.MccCode,
                IsManuallyCategorized = t.IsManuallyCategorized,
                TransactionCategories = t.TransactionCategories.Select(tc => new TransactionCategoryDto
                {
                    CategoryId = tc.CategoryId,
                    CategoryName = tc.Category?.Name,
                    IsBaseCategory = tc.IsBaseCategory
                }).ToList()
            }).ToList();
            return Ok(transactionDtos);
        }

        [HttpGet("category/{categoryId}")]
        public async Task<ActionResult<IEnumerable<TransactionDto>>> GetTransactionsByCategory(Guid categoryId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 15, [FromQuery] bool all = false)
        {
            var currentUserId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var transactions = all
                ? await _transactionRepository.GetTransactionsByCategoriesAsync(categoryId, currentUserId)
                : await _transactionRepository.GetTransactionsByCategoriesAsync(categoryId, currentUserId, pageNumber, pageSize);

            if (!transactions.Any())
            {
                return NotFound("No transactions found for this category and user.");
            }

            var transactionDtos = transactions.Select(t => new TransactionDto
            {
                Id = t.Id,
                Description = t.Description,
                Amount = t.Amount,
                Date = t.Date,
                CategoryId = t.CategoryId,
                CategoryName = t.Category?.Name,
                UserId = t.UserId,
                TransactionType = t.TransactionType,
                MccCode = t.MccCode,
                IsManuallyCategorized = t.IsManuallyCategorized,
                TransactionCategories = t.TransactionCategories.Select(tc => new TransactionCategoryDto
                {
                    CategoryId = tc.CategoryId,
                    CategoryName = tc.Category?.Name,
                    IsBaseCategory = tc.IsBaseCategory
                }).ToList()
            }).ToList();

            return Ok(transactionDtos);
        }

        [HttpPost("update-category")]
        public async Task<IActionResult> UpdateTransactionCategories([FromBody] UpdateTransactionCategoryRequest request)
        {
            if (request.TransactionIds == null || !request.TransactionIds.Any())
            {
                return BadRequest("Transaction IDs list cannot be empty.");
            }

            if (request.CategoryId == Guid.Empty)
            {
                return BadRequest("Category ID cannot be empty.");
            }

            var category = await _categoryRepository.GetByIdAsync(request.CategoryId);
            if (category != null && !category.IsBuiltIn)
            {
                return BadRequest("Base category must be built-in.");
            }

            var validTransactionIds = await _transactionRepository.GetValidExpenseTransactionIdsAsync(request.TransactionIds);
            if (validTransactionIds.Count != request.TransactionIds.Count)
            {
                return BadRequest("Some transaction IDs are invalid or not of type Expense.");
            }

            var currentUserId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var invalidUserTransactions = await _transactionRepository.FindAsync(t => request.TransactionIds.Contains(t.Id) && t.UserId != currentUserId);
            if (invalidUserTransactions.Any())
            {
                return Forbid();
            }

            await _transactionRepository.UpdateCategoryForTransactionsAsync(request.CategoryId, request.TransactionIds);
            return NoContent();
        }
    }
}