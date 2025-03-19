using ExpenseTracker.Data;
using ExpenseTracker.Data.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[Route("api/[controller]")]
[ApiController]
public class TransactionController : ControllerBase
{
    private readonly AppDbContext _context;

    public TransactionController(AppDbContext context)
    {
        _context = context;
    }

    // Получить все транзакции
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Transaction>>> GetTransactions()
    {
        return await _context.Transactions
            .Include(t => t.Category)
            .Include(t => t.User)
            .ToListAsync();
    }

    // Получить одну транзакцию по ID
    [HttpGet("{id}")]
    public async Task<ActionResult<Transaction>> GetTransaction(Guid id)
    {
        var transaction = await _context.Transactions
            .Include(t => t.Category)
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (transaction == null)
        {
            return NotFound();
        }

        return transaction;
    }

    // Добавить новую транзакцию
    [HttpPost]
    public async Task<ActionResult<Transaction>> CreateTransaction([FromForm]Transaction transaction)
    {
        transaction.Id = Guid.NewGuid();
        _context.Transactions.Add(transaction);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetTransaction), new { id = transaction.Id }, transaction);
    }

    // Обновить существующую транзакцию
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateTransaction(Guid id, [FromForm] Transaction transaction)
    {
        if (id == Guid.Empty)
        {
            return BadRequest();
        }
        transaction.Id = id;
        _context.Entry(transaction).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!_context.Transactions.Any(e => e.Id == id))
            {
                return NotFound();
            }
            throw;
        }

        return NoContent();
    }

    // Удалить транзакцию
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTransaction(Guid id)
    {
        var transaction = await _context.Transactions.FindAsync(id);
        if (transaction == null)
        {
            return NotFound();
        }

        _context.Transactions.Remove(transaction);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
