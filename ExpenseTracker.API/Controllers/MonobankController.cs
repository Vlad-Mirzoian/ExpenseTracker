using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
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
        private readonly IDataProtector _protector;

        public MonobankController(
            MonobankService monobankService,
            IUserRepository userRepository,
            IDataProtectionProvider dataProtectionProvider)
        {
            _monobankService = monobankService;
            _userRepository = userRepository;
            _protector = dataProtectionProvider.CreateProtector("MonobankApiTokenProtector");
        }

        [HttpGet("transactions/{userId}")]
        public async Task<ActionResult<List<TransactionDto>>> GetMonobankTransactions(Guid userId)
        {
            try
            {
                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    return NotFound("Користувач не знайдений.");
                }
                if (string.IsNullOrEmpty(user.Token))
                {
                    return BadRequest("У користувача відсутній API-ключ Monobank.");
                }

                string decryptedToken;
                try
                {
                    decryptedToken = _protector.Unprotect(user.Token);
                }
                catch (Exception ex)
                {
                    return BadRequest("Помилка під час розшифрування токена.");
                }
                var now = DateTimeOffset.UtcNow;
                var fromTimestamp = now.AddDays(-30).ToUnixTimeSeconds();
                var toTimestamp = now.ToUnixTimeSeconds();

                var transactions = await _monobankService.GetTransactionsAsync(user.Id, decryptedToken, fromTimestamp, toTimestamp);
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
                return StatusCode(500, $"Помилка під час отримання транзакцій: {ex.Message}");
            }
        }
    }
}