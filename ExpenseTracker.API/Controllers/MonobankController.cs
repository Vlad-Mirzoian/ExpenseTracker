using Microsoft.AspNetCore.Mvc;

namespace ExpenseTracker.API
{
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
        public async Task<ActionResult<List<Transaction>>> GetMonobankTransactions(Guid userId)
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
                return Ok(transactions);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Ошибка при получении транзакций: {ex.Message}");
            }
        }
    }
}