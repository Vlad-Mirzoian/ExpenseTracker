using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

[Route("api/[controller]")]
[ApiController]
public class MonobankController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly MonobankService _monobankService;
    private readonly TransactionService _transactionService;
    private readonly TransactionCategorizationService _categorizationService;

    public MonobankController(AppDbContext context, MonobankService monobankService, TransactionService transactionService, TransactionCategorizationService categorizationService)
    {
        _context = context;
        _monobankService = monobankService;
        _transactionService = transactionService;
        _categorizationService = categorizationService;
    }

    [HttpGet("transactions/{userId}")]
    public async Task<ActionResult<List<Transaction>>> GetMonobankTransactions(Guid userId)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return NotFound("Пользователь не найден.");
            }
            if (string.IsNullOrEmpty(user.Token))
            {
                return BadRequest("У пользователя отсутствует API-ключ Monobank.");
            }

            Console.WriteLine($"Используем API-ключ: {user.Token}");
            var now = DateTimeOffset.UtcNow;
            var fromTimestamp = now.AddDays(-30).ToUnixTimeSeconds(); // Транзакции за последний месяц
            var toTimestamp = now.ToUnixTimeSeconds();

            var transactions = await _monobankService.GetTransactionsAsync(user.Id, user.Token, fromTimestamp, toTimestamp);
            Console.WriteLine($"Ответ Monobank: {JsonConvert.SerializeObject(transactions, Formatting.Indented)}");
            // Классифицируем транзакции по MCC коду
            foreach (var transaction in transactions)
            {
                transaction.CategoryId = await _categorizationService.CategorizeTransactionAsync(transaction.MccCode.GetValueOrDefault());
                Console.WriteLine($"Transaction ID: {transaction.Id}, MCC: {transaction.MccCode}");
            }

            return Ok(transactions);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Ошибка при получении транзакций: {ex.Message}");
        }
    }
}
