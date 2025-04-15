using ExpenseTracker.API.Interface;
using ExpenseTracker.Data;
using ExpenseTracker.Data.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
[Route("api/[controller]")]
[ApiController]
public class CategoryController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ITransactionRepository _transactionRepository;

    public CategoryController(AppDbContext context, ITransactionRepository transactionRepository)
    {
        _context = context;
        _transactionRepository = transactionRepository;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Category>>> GetCategories()
    {
        return await _context.Categories.ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Category>> GetCategory(Guid id)
    {
        var category = await _context.Categories.FindAsync(id);
        if (category == null)
        {
            return NotFound();
        }
        return category;
    }
    [HttpPost]
    public async Task<ActionResult<Category>> CreateCategory([FromForm] Category category)
    {
        category.Id = Guid.NewGuid();
        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetCategory), new { id = category.Id }, category);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateCategory(Guid id, Category category)
    {
        if (id != category.Id)
        {
            return BadRequest("ID mismatch");
        }

        if (string.IsNullOrEmpty(category.Name))
        {
            return BadRequest("Name is required.");
        }
        var existingCategory = await _context.Categories.FindAsync(id);
        if (existingCategory == null)
        {
            return NotFound();
        }

        existingCategory.Name = category.Name;

        var mccCodes = await _context.Transactions
            .Where(t => t.CategoryId == id && t.MccCode.HasValue)
            .Select(t => t.MccCode.Value)
            .ToListAsync();

        existingCategory.MccCodes = mccCodes.Any()
                ? System.Text.Json.JsonSerializer.Serialize(mccCodes.ToArray())
                : "[]";

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!_context.Categories.Any(e => e.Id == id))
            {
                return NotFound();
            }
            throw;
        }

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCategory(Guid id)
    {
        var category = await _context.Categories.FindAsync(id);
        if (category == null)
        {
            return NotFound();
        }

        _context.Categories.Remove(category);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
