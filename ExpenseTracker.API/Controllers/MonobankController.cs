using ExpenseTracker.API.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
[Route("api/[controller]")]
[ApiController]
public class MonobankController : ControllerBase
{
    private readonly MonobankService _monobankService;
    private readonly IUserRepository _userRepository;
    private readonly ITransactionRepository _transactionRepository;
    private readonly TransactionCategorizationService _categorizationService;

    public MonobankController(IUserRepository userRepository, ITransactionRepository transactionRepository, MonobankService monobankService, TransactionCategorizationService categorizationService)
    {
        _userRepository = userRepository;
        _transactionRepository = transactionRepository;
        _monobankService = monobankService;
        _categorizationService = categorizationService;
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
