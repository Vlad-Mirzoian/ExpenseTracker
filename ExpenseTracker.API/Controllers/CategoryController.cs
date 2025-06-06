using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ExpenseTracker.Data.Model;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.API
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class CategoryController : ControllerBase
    {
        private readonly ICategoryRepository _categoryRepository;
        private readonly ITransactionRepository _transactionRepository;

        public CategoryController(ICategoryRepository categoryRepository, ITransactionRepository transactionRepository)
        {
            _categoryRepository = categoryRepository;
            _transactionRepository = transactionRepository;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<CategoryDto>>> GetCategories()
        {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var categories = await _categoryRepository.GetAllAsync(userId);
            var categoryDtos = categories.Select(c => new CategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                MccCodes = c.MccCodesArray,
                IsBuiltIn = c.IsBuiltIn,
                UserId = c.UserId
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

            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            if (!category.IsBuiltIn && category.UserId != userId)
            {
                return Forbid();
            }

            var categoryDto = new CategoryDto
            {
                Id = category.Id,
                Name = category.Name,
                MccCodes = category.MccCodesArray,
                IsBuiltIn = category.IsBuiltIn,
                UserId = category.UserId
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

            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var category = new Category
            {
                Id = Guid.NewGuid(),
                Name = createCategoryDto.Name,
                MccCodes = "[]",
                IsBuiltIn = false,
                UserId = userId
            };

            await _categoryRepository.AddAsync(category);

            if (createCategoryDto.ParentCategoryIds != null && createCategoryDto.ParentCategoryIds.Any())
            {
                foreach (var parentId in createCategoryDto.ParentCategoryIds)
                {
                    var parent = await _categoryRepository.GetByIdAsync(parentId);
                    if (parent == null || !parent.IsBuiltIn)
                    {
                        return BadRequest($"Invalid parent category ID: {parentId}");
                    }
                    await _categoryRepository.AddRelationshipAsync(category.Id, parentId);
                }
            }

            var categoryDto = new CategoryDto
            {
                Id = category.Id,
                Name = category.Name,
                MccCodes = category.MccCodesArray,
                IsBuiltIn = category.IsBuiltIn,
                UserId = category.UserId
            };

            return CreatedAtAction(nameof(GetCategory), new { id = category.Id }, categoryDto);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCategory(Guid id, [FromBody] UpdateCategoryDto updateCategoryDto)
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

            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            if (category.IsBuiltIn || category.UserId != userId)
            {
                return Forbid("Cannot modify built-in or other user's categories.");
            }

            var defaultCategory = await _categoryRepository.GetDefaultCategoryAsync();
            if (defaultCategory == null)
            {
                return StatusCode(500, "Default category 'Інше' not found.");
            }
            var defaultCategoryId = defaultCategory.Id;

            category.Name = updateCategoryDto.Name;
            await _categoryRepository.UpdateAsync(category);

            var currentBaseCategoryIds = await _categoryRepository.GetBaseCategoryIdsAsync(id);
            var newBaseCategoryIds = updateCategoryDto.BaseCategoryIds ?? new List<Guid>();

            foreach (var baseCategoryId in newBaseCategoryIds)
            {
                var baseCategory = await _categoryRepository.GetByIdAsync(baseCategoryId);
                if (baseCategory == null || !baseCategory.IsBuiltIn)
                {
                    return BadRequest($"Invalid base category ID: {baseCategoryId}");
                }
            }

            var baseCategoriesToRemove = currentBaseCategoryIds.Except(newBaseCategoryIds).ToList();
            foreach (var baseCategoryId in baseCategoriesToRemove)
            {
                await _categoryRepository.RemoveRelationshipAsync(id, baseCategoryId);
            }

            var baseCategoriesToAdd = newBaseCategoryIds.Except(currentBaseCategoryIds).ToList();
            foreach (var baseCategoryId in baseCategoriesToAdd)
            {
                await _categoryRepository.AddRelationshipAsync(id, baseCategoryId);
            }

            var allMccCodes = new List<int>();
            foreach (var baseId in newBaseCategoryIds)
            {
                var baseCategory = await _categoryRepository.GetByIdAsync(baseId);
                allMccCodes.AddRange(baseCategory.MccCodesArray);
            }
            category.MccCodesArray = allMccCodes.Distinct().ToArray();
            await _categoryRepository.UpdateAsync(category);

            var transactionIds = await _categoryRepository.GetTransactionIdsByCategoryIdAsync(id, userId);
            if (transactionIds.Any())
            {
                foreach (var baseId in baseCategoriesToRemove)
                {
                    var baseTransactionIds = await _categoryRepository.GetTransactionIdsByCategoryIdAsync(baseId, userId);
                    var transactionsToRemove = transactionIds.Intersect(baseTransactionIds).ToList();
                    if (transactionsToRemove.Any())
                    {
                        await _transactionRepository.RemoveTransactionCategoriesAsync(id, transactionsToRemove);
                    }
                }

                foreach (var baseId in baseCategoriesToAdd)
                {
                    var baseTransactionIds = await _categoryRepository.GetTransactionIdsByCategoryIdAsync(baseId, userId);
                    var transactionsToAdd = baseTransactionIds.Except(transactionIds).ToList();
                    if (transactionsToAdd.Any())
                    {
                        var newCategories = transactionsToAdd.Select(tId => new TransactionCategory
                        {
                            TransactionId = tId,
                            CategoryId = id,
                            IsBaseCategory = false
                        }).ToList();
                        await _transactionRepository.AddTransactionCategoriesAsync(newCategories);
                    }
                }
                await _transactionRepository.ReassignOrphanedTransactionsAsync(transactionIds, defaultCategoryId);
            }

            var categoryDto = new CategoryDto
            {
                Id = category.Id,
                Name = category.Name,
                MccCodes = category.MccCodesArray,
                IsBuiltIn = category.IsBuiltIn,
                UserId = category.UserId
            };

            return Ok(categoryDto);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCategory(Guid id)
        {
            var category = await _categoryRepository.GetByIdAsync(id);
            if (category == null)
            {
                return NotFound();
            }

            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            if (category.IsBuiltIn || category.UserId != userId)
            {
                return Forbid("Cannot delete built-in or other user's categories.");
            }

            var defaultCategory = await _categoryRepository.GetDefaultCategoryAsync();
            if (defaultCategory == null)
            {
                return StatusCode(500, "Default category 'Інше' not found.");
            }

            var transactionIds = await _categoryRepository.GetTransactionIdsByCategoryIdAsync(id, userId);
            if (transactionIds.Any())
            {
                await _transactionRepository.RemoveTransactionCategoriesAsync(id, transactionIds);
                await _transactionRepository.ReassignOrphanedTransactionsAsync(transactionIds, defaultCategory.Id);
            }

            await _categoryRepository.RemoveRelationshipsAsync(id);
            await _categoryRepository.DeleteAsync(id);
            return NoContent();
        }
    }
}