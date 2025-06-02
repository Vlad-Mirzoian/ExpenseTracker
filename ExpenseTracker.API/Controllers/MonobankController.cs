using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseTracker.API
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class MonobankController : ControllerBase
    {
        private readonly MonobankService _monobankService;
        private readonly IUserRepository _userRepository;

        public MonobankController(
            MonobankService monobankService,
            IUserRepository userRepository)
        {
            _monobankService = monobankService;
            _userRepository = userRepository;
        }

        [HttpGet("transactions/{userId}")]
        public async Task<ActionResult<List<TransactionDto>>> GetMonobankTransactions(Guid userId)
        {
            try
            {
                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    return NotFound("Пользователь не найден.");
                }
                if (string.IsNullOrEmpty(user.Token))
                {
                    return BadRequest("У пользователя отсутствует API-ключ Monobank.");
                }

                var now = DateTimeOffset.UtcNow;
                var fromTimestamp = now.AddDays(-30).ToUnixTimeSeconds();
                var toTimestamp = now.ToUnixTimeSeconds();

                var transactions = await _monobankService.GetTransactionsAsync(user.Id, user.Token, fromTimestamp, toTimestamp);
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
            catch (Exception ex)
            {
                return StatusCode(500, $"Ошибка при получении транзакций: {ex.Message}");
            }
        }
    }
}