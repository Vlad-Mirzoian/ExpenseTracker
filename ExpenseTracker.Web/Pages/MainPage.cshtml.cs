using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http.Headers;
using ExpenseTracker.API;
using ExpenseTracker.Data.Model;

namespace ExpenseTracker.Web.Pages
{
    public class MainPageModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IUserRepository _userRepository;
        private readonly ITransactionRepository _transactionRepository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly ILogger<MainPageModel> _logger;

        public User UserInformation { get; set; } = new User();
        public List<CategoryDto> Categories { get; set; } = new List<CategoryDto>();
        public List<TransactionDto> Transactions { get; set; } = new List<TransactionDto>();
        public List<CategorySpendingInfo2> CategorySpending { get; set; } = new List<CategorySpendingInfo2>();
        public List<CategorySpendingInfo2> IncomeByCategory { get; set; } = new List<CategorySpendingInfo2>();
        public List<(string Label, decimal TotalAmount)> ExpenseChartData { get; set; } = new List<(string, decimal)>();
        public List<(string Label, decimal TotalAmount)> IncomeChartData { get; set; } = new List<(string, decimal)>();
        public decimal TotalIncome { get; set; }
        public string UpdateCategoryError { get; set; }
        public string CreateCategoryError { get; set; }
        [BindProperty(SupportsGet = true)]
        public string ChartMode { get; set; } = "BuiltIn";
        [BindProperty(SupportsGet = true)]
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 15;
        public bool HasMoreTransactions { get; set; }

        [BindProperty]
        public string NewCategoryName { get; set; }
        [BindProperty]
        public List<Guid> BaseCategoryIds { get; set; } = new List<Guid>();

        public MainPageModel(
            IHttpClientFactory httpClientFactory,
            IUserRepository userRepository,
            ITransactionRepository transactionRepository,
            ICategoryRepository categoryRepository,
            ILogger<MainPageModel> logger)
        {
            _httpClientFactory = httpClientFactory;
            _userRepository = userRepository;
            _transactionRepository = transactionRepository;
            _categoryRepository = categoryRepository;
            _logger = logger;
        }

        public async Task<IActionResult> OnGetAsync(Guid? id)
        {
            var jwt = Request.Cookies["jwt"];
            if (string.IsNullOrEmpty(jwt))
            {
                return RedirectToPage("/Auth/Login");
            }

            var client = _httpClientFactory.CreateClient("ExpenseTrackerApi");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);

            try
            {
                var responseUser = await client.GetAsync("api/auth/me");
                if (!responseUser.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Failed to fetch user info: {StatusCode}", responseUser.StatusCode);
                    return RedirectToPage("/Auth/Login");
                }

                UserInformation = await responseUser.Content.ReadFromJsonAsync<User>() ?? new User();

                var user = await _userRepository.GetByIdAsync(UserInformation.Id);
                if (user == null)
                {
                    _logger.LogWarning("User not found: {UserId}", UserInformation.Id);
                    return RedirectToPage("/Auth/Login");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching user info");
                return RedirectToPage("/Auth/Login");
            }

            var responseCategories = await client.GetAsync("api/category");
            if (responseCategories.IsSuccessStatusCode)
            {
                Categories = await responseCategories.Content.ReadFromJsonAsync<List<CategoryDto>>() ?? new List<CategoryDto>();
            }
            else
            {
                _logger.LogWarning("Failed to fetch categories: {StatusCode}", responseCategories.StatusCode);
            }

            if (id.HasValue)
            {
                var query = $"api/transaction/category/{id.Value}?pageNumber={PageNumber}&pageSize={PageSize}";
                _logger.LogInformation("Fetching transactions for category {CategoryId}, page {PageNumber}, size {PageSize}", id.Value, PageNumber, PageSize);
                var response = await client.GetAsync(query);
                if (response.IsSuccessStatusCode)
                {
                    Transactions = await response.Content.ReadFromJsonAsync<List<TransactionDto>>() ?? new List<TransactionDto>();
                    _logger.LogInformation("Fetched {TransactionCount} transactions", Transactions.Count);

                    // Check if more transactions exist
                    var nextPageResponse = await client.GetAsync($"api/transaction/category/{id.Value}?pageNumber={PageNumber + 1}&pageSize={PageSize}");
                    HasMoreTransactions = nextPageResponse.IsSuccessStatusCode && 
                        (await nextPageResponse.Content.ReadFromJsonAsync<List<TransactionDto>>())?.Any() == true;
                    _logger.LogInformation("Has more transactions: {HasMore}", HasMoreTransactions);
                }
                else
                {
                    _logger.LogWarning("Failed to fetch transactions for category {CategoryId}: {StatusCode}", id.Value, response.StatusCode);
                }

                ExpenseChartData = Transactions
                    .Where(t => t.TransactionType == "Expense")
                    .GroupBy(t => t.Description)
                    .Select(g => (Label: g.Key, TotalAmount: g.Sum(t => Math.Abs(t.Amount))))
                    .ToList();

                IncomeChartData = Transactions
                    .Where(t => t.TransactionType == "Income")
                    .GroupBy(t => t.Description)
                    .Select(g => (Label: g.Key, TotalAmount: g.Sum(t => t.Amount)))
                    .ToList();

                return Page();
            }
            else
            {
                var responseTransactions = await client.GetAsync($"api/monobank/transactions/{UserInformation.Id}");
                if (responseTransactions.IsSuccessStatusCode)
                {
                    Transactions = await responseTransactions.Content.ReadFromJsonAsync<List<TransactionDto>>() ?? new List<TransactionDto>();
                    _logger.LogInformation("Fetched {TransactionCount} transactions for user {UserId}", Transactions.Count, UserInformation.Id);
                }
                else
                {
                    _logger.LogWarning("Failed to fetch transactions for user {UserId}: {StatusCode}", UserInformation.Id, responseTransactions.StatusCode);
                }

                var expenses = Transactions.Where(t => t.TransactionType == "Expense").ToList();
                var income = Transactions.Where(t => t.TransactionType == "Income").ToList();


                var totalSpending = expenses.Sum(t => Math.Abs(t.Amount));
                // TotalIncome = income.Sum(t => t.Amount);
                var totalIncomeAmount = income.Sum(t => t.Amount);
                var categoryNames = Categories.ToDictionary(c => c.Id, c => c.Name);

                if (ChartMode == "Custom")
                {
                    var customCategoryIds = Categories.Where(c => !c.IsBuiltIn).Select(c => c.Id).ToHashSet();
                    var customExpenses = expenses
                        .SelectMany(t => t.TransactionCategories
                            .Where(tc => customCategoryIds.Contains(tc.CategoryId))
                            .Select(tc => new { Transaction = t, Category = tc }))
                        .GroupBy(x => x.Category.CategoryId)
                        .Select(g => new CategorySpendingInfo2
                        {
                            CategoryId = g.Key,
                            CategoryName = categoryNames.ContainsKey(g.Key) ? categoryNames[g.Key] : "Невідома категорія",
                            TotalSpending = g.Sum(x => Math.Abs(x.Transaction.Amount)),
                            Percentage = totalSpending > 0 ? (double)(g.Sum(x => Math.Abs(x.Transaction.Amount)) / totalSpending * 100) : 0,
                            IsBuiltIn = false
                        })
                        .ToList();

                    var customIncome = income
                        .SelectMany(t => t.TransactionCategories
                            .Where(tc => customCategoryIds.Contains(tc.CategoryId))
                            .Select(tc => new { Transaction = t, Category = tc }))
                        .GroupBy(x => x.Category.CategoryId)
                        .Select(g => new CategorySpendingInfo2
                        {
                            CategoryId = g.Key,
                            CategoryName = categoryNames.ContainsKey(g.Key) ? categoryNames[g.Key] : "Невідома категорія",
                            TotalSpending = g.Sum(x => x.Transaction.Amount),
                            Percentage = totalIncomeAmount > 0 ? (double)(g.Sum(x => x.Transaction.Amount) / totalIncomeAmount * 100) : 0,
                            IsBuiltIn = false
                        })
                        .ToList();

                    var customCategorizedExpenseAmount = customExpenses.Sum(x => x.TotalSpending);
                    if (totalSpending > customCategorizedExpenseAmount)
                    {
                        customExpenses.Add(new CategorySpendingInfo2
                        {
                            CategoryId = Guid.Empty,
                            CategoryName = "Інше",
                            TotalSpending = totalSpending - customCategorizedExpenseAmount,
                            Percentage = totalSpending > 0 ? (double)((totalSpending - customCategorizedExpenseAmount) / totalSpending * 100) : 0,
                            IsBuiltIn = true
                        });
                    }

                    var customCategorizedIncomeAmount = customIncome.Sum(x => x.TotalSpending);
                    if (totalIncomeAmount > customCategorizedIncomeAmount)
                    {
                        customIncome.Add(new CategorySpendingInfo2
                        {
                            CategoryId = Guid.Empty,
                            CategoryName = "Інше",
                            TotalSpending = totalIncomeAmount - customCategorizedIncomeAmount,
                            Percentage = totalIncomeAmount > 0 ? (double)((totalIncomeAmount - customCategorizedIncomeAmount) / totalIncomeAmount * 100) : 0,
                            IsBuiltIn = true
                        });
                    }

                    CategorySpending = customExpenses.OrderByDescending(x => x.TotalSpending).ToList();
                    IncomeByCategory = customIncome.OrderByDescending(x => x.TotalSpending).ToList();
                }
                else
                {
                    var builtInCategoryIds = Categories.Where(c => c.IsBuiltIn).Select(c => c.Id).ToHashSet();
                    CategorySpending = expenses
                        .SelectMany(t => t.TransactionCategories
                            .Where(tc => builtInCategoryIds.Contains(tc.CategoryId))
                            .Select(tc => new { Transaction = t, Category = tc }))
                        .GroupBy(x => x.Category.CategoryId)
                        .Select(g => new CategorySpendingInfo2
                        {
                            CategoryId = g.Key,
                            CategoryName = categoryNames.ContainsKey(g.Key) ? categoryNames[g.Key] : "Невідома категорія",
                            TotalSpending = g.Sum(x => Math.Abs(x.Transaction.Amount)),
                            Percentage = totalSpending > 0 ? (double)(g.Sum(x => Math.Abs(x.Transaction.Amount)) / totalSpending * 100) : 0,
                            IsBuiltIn = true
                        })
                        .OrderByDescending(x => x.TotalSpending)
                        .ToList();

                    IncomeByCategory = income
                        .SelectMany(t => t.TransactionCategories
                            .Where(tc => builtInCategoryIds.Contains(tc.CategoryId))
                            .Select(tc => new { Transaction = t, Category = tc }))
                        .GroupBy(x => x.Category.CategoryId)
                        .Select(g => new CategorySpendingInfo2
                        {
                            CategoryId = g.Key,
                            CategoryName = categoryNames.ContainsKey(g.Key) ? categoryNames[g.Key] : "Невідома категорія",
                            TotalSpending = g.Sum(x => x.Transaction.Amount),
                            Percentage = totalIncomeAmount > 0 ? (double)(g.Sum(x => x.Transaction.Amount) / totalIncomeAmount * 100) : 0,
                            IsBuiltIn = true
                        })
                        .OrderByDescending(x => x.TotalSpending)
                        .ToList();
                }

                ExpenseChartData = CategorySpending
                    .Select(x => (Label: x.CategoryName, TotalAmount: x.TotalSpending))
                    .ToList();

                IncomeChartData = IncomeByCategory
                    .Select(x => (Label: x.CategoryName, TotalAmount: x.TotalSpending))
                    .ToList();

                return Page();
            }
        }

        public IActionResult OnPostLogout()
        {
            Response.Cookies.Delete("jwt");
            return RedirectToPage("/Auth/Login");
        }

        public async Task<IActionResult> OnPostUpdateTransactionCategoryAsync(Guid transactionId, Guid categoryId)
        {
            var jwt = Request.Cookies["jwt"];
            if (string.IsNullOrEmpty(jwt))
            {
                return RedirectToPage("/Auth/Login");
            }

            if (categoryId == Guid.Empty)
            {
                UpdateCategoryError = "Виберіть категорію.";
                return RedirectToPage(new { id = Request.Query["id"], pageNumber = PageNumber });
            }

            var client = _httpClientFactory.CreateClient("ExpenseTrackerApi");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);

            var request = new UpdateTransactionCategoryRequest
            {
                CategoryId = categoryId,
                TransactionIds = new List<Guid> { transactionId }
            };

            try
            {
                var response = await client.PostAsJsonAsync("api/transaction/update-category", request);

                if (response.IsSuccessStatusCode)
                {
                    return RedirectToPage(new { id = Request.Query["id"], pageNumber = PageNumber });
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                UpdateCategoryError = $"Помилка: {errorContent}";
            }
            catch (Exception ex)
            {
                UpdateCategoryError = $"Помилка: {ex.Message}";
                _logger.LogError(ex, "Error updating transaction category");
            }

            return RedirectToPage(new { id = Request.Query["id"], pageNumber = PageNumber });
        }

        public async Task<IActionResult> OnPostCreateCategoryAsync()
        {
            var jwt = Request.Cookies["jwt"];
            if (string.IsNullOrEmpty(jwt))
            {
                return RedirectToPage("/Auth/Login");
            }

            if (string.IsNullOrWhiteSpace(NewCategoryName))
            {
                CreateCategoryError = "Назва категорії не може бути порожньою.";
                return RedirectToPage(new { id = Request.Query["id"], pageNumber = PageNumber });
            }

            var client = _httpClientFactory.CreateClient("ExpenseTrackerApi");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);

            var createCategoryDto = new CreateCategoryDto
            {
                Name = NewCategoryName,
                ParentCategoryIds = BaseCategoryIds,
                IsBuiltIn = false
            };

            try
            {
                var response = await client.PostAsJsonAsync("api/category", createCategoryDto);
                if (response.IsSuccessStatusCode)
                {
                    return RedirectToPage(new { id = Request.Query["id"], pageNumber = PageNumber });
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                CreateCategoryError = $"Помилка створення категорії: {errorContent}";
            }
            catch (Exception ex)
            {
                CreateCategoryError = $"Помилка: {ex.Message}";
                _logger.LogError(ex, "Error creating category");
            }

            return RedirectToPage(new { id = Request.Query["id"], pageNumber = PageNumber });
        }
    }

    public record CategorySpendingInfo2
    {
        public Guid CategoryId { get; init; }
        public string CategoryName { get; init; }
        public decimal TotalSpending { get; init; }
        public double Percentage { get; init; }
        public bool IsBuiltIn { get; init; }
    }
}