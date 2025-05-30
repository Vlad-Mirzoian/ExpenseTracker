using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;


namespace ExpenseTracker.API
{
    [Route("api/[controller]")]
    [ApiController]
    public class TransactionController : ControllerBase
    {
        private readonly ITransactionRepository _transactionRepository;

        public TransactionController(
            ITransactionRepository transactionRepository)
        {
            _transactionRepository = transactionRepository ?? throw new ArgumentNullException(nameof(transactionRepository));
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<TransactionDto>>> GetTransactions()
        {
            var currentUserId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var transactions = await _transactionRepository.FindAsync(t => t.UserId == currentUserId);
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
                IsManuallyCategorized = t.IsManuallyCategorized
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
                IsManuallyCategorized = transaction.IsManuallyCategorized
            };
            return Ok(transactionDto);
        }

        [HttpGet("category/{categoryId}")]
        public async Task<ActionResult<List<TransactionDto>>> GetTransactionByCategory(Guid categoryId)
        {
            var transactions = await _transactionRepository.GetTransactionsByCategoriesAsync(categoryId);
            var currentUserId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var transactionDtos = transactions
                .Where(t => t.UserId == currentUserId)
                .Select(t => new TransactionDto
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
                    IsManuallyCategorized = t.IsManuallyCategorized
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
                IsManuallyCategorized = createDto.IsManuallyCategorized
            };

            await _transactionRepository.AddAsync(transaction);

            var transactionDto = new TransactionDto
            {
                Id = transaction.Id,
                Description = transaction.Description,
                Amount = transaction.Amount,
                Date = transaction.Date,
                CategoryId = transaction.CategoryId,
                UserId = transaction.UserId,
                TransactionType = transaction.TransactionType,
                MccCode = transaction.MccCode,
                IsManuallyCategorized = transaction.IsManuallyCategorized
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

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTransaction(Guid id)
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

            await _transactionRepository.DeleteAsync(id);
            return NoContent();
        }
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
    }

    public class CreateTransactionDto
    {
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
        public Guid CategoryId { get; set; }
        public List<Guid> TransactionIds { get; set; }
    }
}