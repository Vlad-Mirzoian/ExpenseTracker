using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using ExpenseTracker.Data.Model;

namespace ExpenseTracker.API
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class CategoryController : ControllerBase
    {
        private readonly ICategoryRepository _categoryRepository;
        private readonly ITransactionRepository _transactionRepository;

        public CategoryController(
            ICategoryRepository categoryRepository,
            ITransactionRepository transactionRepository)
        {
            _categoryRepository = categoryRepository ?? throw new ArgumentNullException(nameof(categoryRepository));
            _transactionRepository = transactionRepository ?? throw new ArgumentNullException(nameof(transactionRepository));
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<CategoryDto>>> GetCategories()
        {
            var categories = await _categoryRepository.GetAllAsync();
            var categoryDtos = categories.Select(c => new CategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                MccCodes = c.MccCodesArray,
                IsBuiltIn = c.IsBuiltIn
            }).ToList();
            return Ok(categoryDtos);
        }

        [HttpGet("built-in")]
        public async Task<ActionResult<IEnumerable<CategoryDto>>> GetBuiltInCategories()
        {
            var categories = await _categoryRepository.FindAsync(c => c.IsBuiltIn);
            var categoryDtos = categories.Select(c => new CategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                MccCodes = c.MccCodesArray,
                IsBuiltIn = c.IsBuiltIn
            }).ToList();
            return Ok(categoryDtos);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<CategoryDto>> GetCategory(Guid id)
        {
            var category = await _categoryRepository.GetByIdAsync(id);
            if (category == null)
            {
                return NotFound();
            }

            var categoryDto = new CategoryDto
            {
                Id = category.Id,
                Name = category.Name,
                MccCodes = category.MccCodesArray,
                IsBuiltIn = category.IsBuiltIn
            };
            return Ok(categoryDto);
        }

        [HttpPost]
        public async Task<ActionResult<CategoryDto>> CreateCategory([FromBody] CreateCategoryDto createCategoryDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Compute MCC codes from parent categories
            int[] mccCodes = Array.Empty<int>();
            if (createCategoryDto.ParentCategoryIds != null && createCategoryDto.ParentCategoryIds.Any())
            {
                var parentCategories = await _categoryRepository.FindAsync(c => createCategoryDto.ParentCategoryIds.Contains(c.Id));
                mccCodes = parentCategories.SelectMany(c => c.MccCodesArray ?? Array.Empty<int>()).Distinct().ToArray();
            }

            var category = new Category
            {
                Id = Guid.NewGuid(),
                Name = createCategoryDto.Name,
                MccCodes = JsonSerializer.Serialize(mccCodes),
                IsBuiltIn = createCategoryDto.IsBuiltIn
            };

            await _categoryRepository.AddAsync(category);

            // Add parent relationships
            if (createCategoryDto.ParentCategoryIds != null && createCategoryDto.ParentCategoryIds.Any())
            {
                foreach (var parentId in createCategoryDto.ParentCategoryIds)
                {
                    await _categoryRepository.AddParentRelationshipAsync(category.Id, parentId);
                }
            }

            var categoryDto = new CategoryDto
            {
                Id = category.Id,
                Name = category.Name,
                MccCodes = category.MccCodesArray,
                IsBuiltIn = category.IsBuiltIn
            };

            return CreatedAtAction(nameof(GetCategory), new { id = category.Id }, categoryDto);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCategory(Guid id, [FromBody] CreateCategoryDto updateCategoryDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var category = await _categoryRepository.GetByIdAsync(id);
            if (category == null)
            {
                return NotFound();
            }

            if (category.IsBuiltIn)
            {
                return BadRequest("Built-in categories cannot be modified.");
            }

            // Compute MCC codes from parent categories
            int[] mccCodes = Array.Empty<int>();
            if (updateCategoryDto.ParentCategoryIds != null && updateCategoryDto.ParentCategoryIds.Any())
            {
                var parentCategories = await _categoryRepository.FindAsync(c => updateCategoryDto.ParentCategoryIds.Contains(c.Id));
                mccCodes = parentCategories.SelectMany(c => c.MccCodesArray ?? Array.Empty<int>()).Distinct().ToArray();
            }

            category.Name = updateCategoryDto.Name;
            category.MccCodes = JsonSerializer.Serialize(mccCodes);

            await _categoryRepository.UpdateAsync(category);

            // Update parent relationships
            await _categoryRepository.RemoveParentRelationshipsAsync(id);
            if (updateCategoryDto.ParentCategoryIds != null && updateCategoryDto.ParentCategoryIds.Any())
            {
                foreach (var parentId in updateCategoryDto.ParentCategoryIds)
                {
                    await _categoryRepository.AddParentRelationshipAsync(id, parentId);
                }
            }

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCategory(Guid id)
        {
            var category = await _categoryRepository.GetByIdAsync(id);
            if (category == null)
            {
                return NotFound();
            }

            if (category.IsBuiltIn)
            {
                return BadRequest("Built-in categories cannot be deleted.");
            }

            var defaultCategory = await _categoryRepository.GetDefaultCategoryAsync();
            if (defaultCategory == null)
            {
                return StatusCode(500, "Default category 'Інше' not found.");
            }

            var transactions = await _transactionRepository.GetTransactionsByCategoriesAsync(id);
            var transactionIds = transactions.Select(t => t.Id).ToList();

            if (transactionIds.Any())
            {
                await _transactionRepository.UpdateCategoryForTransactionsAsync(defaultCategory.Id, transactionIds);
            }

            await _categoryRepository.DeleteAsync(category.Id);
            return NoContent();
        }
    }

    public class CategoryDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public int[] MccCodes { get; set; }
        public bool IsBuiltIn { get; set; }
    }

    public class CreateCategoryDto
    {
        public string Name { get; set; }
        public Guid[] ParentCategoryIds { get; set; }
        public bool IsBuiltIn { get; set; }
    }
}