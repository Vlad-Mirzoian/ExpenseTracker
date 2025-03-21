using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[Route("api/[controller]")]
[ApiController]
public class MerchantAliasController : ControllerBase
{
    private readonly AppDbContext _context;

    public MerchantAliasController(AppDbContext context)
    {
        _context = context;
    }

    // Получить все привязки терминалов
    [HttpGet]
    public async Task<ActionResult<IEnumerable<MerchantAlias>>> GetAliases()
    {
        return await _context.MerchantAliases.ToListAsync();
    }

    // Добавить новую привязку
    [HttpPost]
    public async Task<ActionResult<MerchantAlias>> CreateAlias([FromBody] MerchantAlias alias)
    {
        alias.Id = Guid.NewGuid();
        _context.MerchantAliases.Add(alias);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetAliases), new { id = alias.Id }, alias);
    }

    // Удалить привязку
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteAlias(Guid id)
    {
        var alias = await _context.MerchantAliases.FindAsync(id);
        if (alias == null)
        {
            return NotFound();
        }

        _context.MerchantAliases.Remove(alias);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
