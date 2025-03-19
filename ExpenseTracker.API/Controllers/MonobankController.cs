using ExpenseTracker.Data;
using ExpenseTracker.Data.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[Route("api/[controller]")]
[ApiController]
public class MonobankController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly MonobankService _monobankService;

    public MonobankController(AppDbContext context, MonobankService monobankService)
    {
        _context = context;
        _monobankService = monobankService;
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

            var transactions = await _monobankService.GetTransactionsAsync(user.Id,user.Token, fromTimestamp, toTimestamp);
            return Ok(transactions);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Ошибка при получении транзакций: {ex.Message}");
        }
    }
}
