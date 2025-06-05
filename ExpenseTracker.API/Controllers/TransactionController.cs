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

        [HttpGet("{id}")]
        public async Task<ActionResult<TransactionDto>> GetTransaction(Guid id)
        {
            var transaction = await _transactionRepository.GetByIdAsync(id);
            if (transaction == null)
            {
                return NotFound();
            }

            var currentUserId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            if (transaction.UserId != currentUserId)
            {
                return Forbid();
            }

            var transactionDto = new TransactionDto
            {
                Id = transaction.Id,
                Description = transaction.Description,
                Amount = transaction.Amount,
                Date = transaction.Date,
                CategoryId = transaction.CategoryId,
                CategoryName = transaction.Category?.Name,
                UserId = transaction.UserId,
                TransactionType = transaction.TransactionType,
                MccCode = transaction.MccCode,
                IsManuallyCategorized = transaction.IsManuallyCategorized,
                TransactionCategories = transaction.TransactionCategories.Select(tc => new TransactionCategoryDto
                {
                    CategoryId = tc.CategoryId,
                    CategoryName = tc.Category?.Name,
                    IsBaseCategory = tc.IsBaseCategory
                }).ToList()
            };
            return Ok(transactionDto);
        }

        [HttpGet("category/{categoryId}")]
        public async Task<ActionResult<IEnumerable<TransactionDto>>> GetTransactionByCategory(Guid categoryId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 15)
        {
            var currentUserId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var transactions = await _transactionRepository.GetTransactionsByCategoriesAsync(categoryId, pageNumber, pageSize);

            // Filter transactions by UserId
            transactions = transactions.Where(t => t.UserId == currentUserId).ToList();
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

        [HttpGet("all-by-category/{categoryId}")]
        public async Task<ActionResult<IEnumerable<TransactionDto>>> GetAllTransactionsByCategory(Guid categoryId)
        {
            var currentUserId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var transactions = await _transactionRepository.GetTransactionsByCategoriesAsync(categoryId);

            // Filter transactions by UserId
            transactions = transactions.Where(t => t.UserId == currentUserId).ToList();
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

        [HttpPost]
        public async Task<ActionResult<TransactionDto>> CreateTransaction([FromBody] CreateTransactionDto createDto)
        {
            var currentUserId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            if (createDto.UserId != currentUserId)
            {
                return Forbid();
            }

            if (createDto.Amount <= 0)
            {
                return BadRequest("Amount must be positive.");
            }

            if (createDto.CategoryId == Guid.Empty)
            {
                return BadRequest("Category ID cannot be empty.");
            }

            var category = await _categoryRepository.GetByIdAsync(createDto.CategoryId);
            if (category != null && !category.IsBuiltIn)
            {
                return BadRequest("Base category must be built-in.");
            }

            var transaction = new Transaction
            {
                Id = Guid.NewGuid(),
                Description = createDto.Description,
                Amount = createDto.Amount,
                Date = createDto.Date,
                CategoryId = createDto.CategoryId,
                UserId = createDto.UserId,
                TransactionType = createDto.TransactionType,
                MccCode = createDto.MccCode,
                IsManuallyCategorized = true
            };

            await _transactionRepository.AddAsync(transaction);

            var transactionDto = new TransactionDto
            {
                Id = transaction.Id,
                Description = transaction.Description,
                Amount = transaction.Amount,
                Date = transaction.Date,
                CategoryId = transaction.CategoryId,
                CategoryName = transaction.Category?.Name,
                UserId = transaction.UserId,
                TransactionType = transaction.TransactionType,
                MccCode = transaction.MccCode,
                IsManuallyCategorized = transaction.IsManuallyCategorized,
                TransactionCategories = transaction.TransactionCategories.Select(tc => new TransactionCategoryDto
                {
                    CategoryId = tc.CategoryId,
                    CategoryName = tc.Category?.Name,
                    IsBaseCategory = tc.IsBaseCategory
                }).ToList()
            };

            return CreatedAtAction(nameof(GetTransaction), new { id = transaction.Id }, transactionDto);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTransaction(Guid id, [FromBody] UpdateTransactionDto updateDto)
        {
            if (id != updateDto.Id)
            {
                return BadRequest("ID mismatch.");
            }

            var transaction = await _transactionRepository.GetByIdAsync(id);
            if (transaction == null)
            {
                return NotFound();
            }

            var currentUserId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            if (transaction.UserId != currentUserId)
            {
                return Forbid();
            }

            if (updateDto.Amount <= 0)
            {
                return BadRequest("Amount must be positive.");
            }

            if (updateDto.CategoryId == Guid.Empty)
            {
                return BadRequest("Category ID cannot be empty.");
            }

            var category = await _categoryRepository.GetByIdAsync(updateDto.CategoryId);
            if (category != null && !category.IsBuiltIn)
            {
                return BadRequest("Base category must be built-in.");
            }

            transaction.Description = updateDto.Description;
            transaction.Amount = updateDto.Amount;
            transaction.Date = updateDto.Date;
            transaction.CategoryId = updateDto.CategoryId;
            transaction.TransactionType = updateDto.TransactionType;
            transaction.MccCode = updateDto.MccCode;
            transaction.IsManuallyCategorized = updateDto.IsManuallyCategorized;

            try
            {
                await _transactionRepository.UpdateAsync(transaction);
                return NoContent();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (await _transactionRepository.GetByIdAsync(id) == null)
                {
                    return NotFound();
                }
                throw;
            }
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