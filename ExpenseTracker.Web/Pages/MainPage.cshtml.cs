using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http.Headers;
using ExpenseTracker.API;
using ExpenseTracker.Data.Model;

using System.Text.Json;

namespace ExpenseTracker.Web.Pages
{
    public class MainPageModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<MainPageModel> _logger;

        public User UserInformation { get; set; } = new User();
        public List<CategoryDto> Categories { get; set; } = new List<CategoryDto>();
        public List<TransactionDto> Transactions { get; set; } = new List<TransactionDto>();
        public List<CategorySpendingInfo> CategorySpending { get; set; } = new List<CategorySpendingInfo>();
        public List<CategorySpendingInfo> IncomeByCategory { get; set; } = new List<CategorySpendingInfo>();
        public List<TransactionDescriptionInfo> ExpenseDescriptionChartData { get; set; } = new List<TransactionDescriptionInfo>();
        public List<TransactionDescriptionInfo> IncomeDescriptionChartData { get; set; } = new List<TransactionDescriptionInfo>();
        public string? ModelError { get; set; }
        public string? CategoryDeleteError { get; set; }
        public string? CategoryEditError { get; set; }
        public string? ChangeCredentialsError { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 15;
        public bool HasMoreTransactions { get; set; }

        [BindProperty]
        public string? NewCategoryName { get; set; }
        [BindProperty]
        public List<Guid> BaseCategoryIds { get; set; } = new List<Guid>();
        [BindProperty]
        public Guid EditCategoryId { get; set; }
        [BindProperty]
        public UpdateUserCredentialsRequest ChangeCredentialsInput { get; set; }

        public MainPageModel(
            IHttpClientFactory httpClientFactory,
            ILogger<MainPageModel> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task<IActionResult> OnGetAsync(Guid? id, int pageNumber = 1)
        {
            PageNumber = pageNumber < 1 ? 1 : pageNumber;

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
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching user info");
                return RedirectToPage("/Auth/Login");
            }

            await LoadCategoriesAsync(client);

            if (id.HasValue)
            {
                var query = $"api/transaction/category/{id.Value}?pageNumber={PageNumber}&pageSize={PageSize}";
                _logger.LogInformation("Fetching transactions for category {CategoryId}, page {PageNumber}, size {PageSize}", id.Value, PageNumber, PageSize);
                try
                {
                    var response = await client.GetAsync(query);
                    if (response.IsSuccessStatusCode)
                    {
                        Transactions = await response.Content.ReadFromJsonAsync<List<TransactionDto>>() ?? new List<TransactionDto>();
                        _logger.LogInformation("Fetched {TransactionCount} transactions for category {CategoryId}", Transactions.Count, id.Value);

                        var nextPageQuery = $"api/transaction/category/{id.Value}?pageNumber={PageNumber + 1}&pageSize={PageSize}";
                        _logger.LogInformation("Checking next page: {Query}", nextPageQuery);
                        var nextPageResponse = await client.GetAsync(nextPageQuery);
                        if (nextPageResponse.IsSuccessStatusCode)
                        {
                            var nextPageTransactions = await nextPageResponse.Content.ReadFromJsonAsync<List<TransactionDto>>() ?? new List<TransactionDto>();
                            HasMoreTransactions = nextPageTransactions.Any();
                            _logger.LogInformation("Next page has {Count} transactions. HasMoreTransactions: {HasMore}", nextPageTransactions.Count, HasMoreTransactions);
                        }
                        else if (nextPageResponse.StatusCode == System.Net.HttpStatusCode.NotFound)
                        {
                            HasMoreTransactions = false;
                            _logger.LogInformation("No more transactions for category {CategoryId} on page {NextPage}", id.Value, PageNumber + 1);
                        }
                        else
                        {
                            _logger.LogWarning("Failed to check next page for category {CategoryId}: {StatusCode}", id.Value, nextPageResponse.StatusCode);
                            HasMoreTransactions = false;
                        }
                    }
                    else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        _logger.LogInformation("No transactions found for category {CategoryId}", id.Value);
                        Transactions = new List<TransactionDto>();
                        HasMoreTransactions = false;
                    }
                    else
                    {
                        _logger.LogWarning("Failed to fetch transactions for category {CategoryId}: {StatusCode}", id.Value, response.StatusCode);
                        var errorContent = await response.Content.ReadAsStringAsync();
                        _logger.LogWarning("Error details: {ErrorContent}", errorContent);
                        ModelError = "Не вдалося завантажити транзакції.";
                        Transactions = new List<TransactionDto>();
                        HasMoreTransactions = false;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error fetching transactions for category {CategoryId}", id.Value);
                    ModelError = "Помилка завантаження транзакцій.";
                    Transactions = new List<TransactionDto>();
                    HasMoreTransactions = false;
                }

                var chartQuery = $"api/transaction/all-by-category/{id.Value}";
                var chartResponse = await client.GetAsync(chartQuery);
                if (chartResponse.IsSuccessStatusCode)
                {
                    var allTransactions = await chartResponse.Content.ReadFromJsonAsync<List<TransactionDto>>() ?? new List<TransactionDto>();
                    var categoryTransactions = allTransactions
                        .Where(t => t.TransactionCategories.Any(tc => tc.CategoryId == id.Value))
                        .ToList();

                    ExpenseDescriptionChartData = categoryTransactions
                        .Where(t => t.TransactionType == "Expense")
                        .GroupBy(t => t.Description)
                        .Select(g => new TransactionDescriptionInfo
                        {
                            Description = g.Key,
                            TotalAmount = g.Sum(t => Math.Abs(t.Amount)),
                            TransactionType = "Expense"
                        })
                        .OrderByDescending(t => t.TotalAmount)
                        .ToList();

                    IncomeDescriptionChartData = categoryTransactions
                        .Where(t => t.TransactionType == "Income")
                        .GroupBy(t => t.Description)
                        .Select(g => new TransactionDescriptionInfo
                        {
                            Description = g.Key,
                            TotalAmount = g.Sum(t => t.Amount),
                            TransactionType = "Income"
                        })
                        .OrderByDescending(t => t.TotalAmount)
                        .ToList();

                    _logger.LogInformation("ExpenseDescriptionChartData: {Data}", JsonSerializer.Serialize(ExpenseDescriptionChartData));
                    _logger.LogInformation("IncomeDescriptionChartData: {Data}", JsonSerializer.Serialize(IncomeDescriptionChartData));
                }
                else
                {
                    _logger.LogWarning("Failed to fetch transactions for chart: {StatusCode}", chartResponse.StatusCode);
                    ModelError = "Не вдалося завантажити дані для графіків.";
                }

                return Page();
            }
            else
            {
                await LoadTransactionsAsync(client);

                var expenses = Transactions.Where(t => t.TransactionType == "Expense").ToList();
                var income = Transactions.Where(t => t.TransactionType == "Income").ToList();

                var totalSpending = expenses.Sum(t => Math.Abs(t.Amount));
                var totalIncomeAmount = income.Sum(t => t.Amount);
                var categoryNames = Categories.ToDictionary(c => c.Id, c => c.Name);
                var builtInCategoryIds = Categories.Where(c => c.IsBuiltIn).Select(c => c.Id).ToHashSet();

                CategorySpending = expenses
                    .SelectMany(t => t.TransactionCategories
                        .Where(tc => builtInCategoryIds.Contains(tc.CategoryId))
                        .Select(tc => new { Transaction = t, Category = tc }))
                    .GroupBy(x => x.Category.CategoryId)
                    .Select(g => new CategorySpendingInfo
                    {
                        CategoryId = g.Key,
                        CategoryName = categoryNames.GetValueOrDefault(g.Key, "Невідома категорія"),
                        TotalAmount = g.Sum(x => Math.Abs(x.Transaction.Amount)),
                        Percentage = totalSpending > 0 ? (double)(g.Sum(x => Math.Abs(x.Transaction.Amount)) / totalSpending * 100) : 0,
                        IsBuiltIn = true
                    })
                    .OrderByDescending(c => c.TotalAmount)
                    .ToList();

                IncomeByCategory = income
                    .SelectMany(t => t.TransactionCategories
                        .Where(tc => builtInCategoryIds.Contains(tc.CategoryId))
                        .Select(tc => new { Transaction = t, Category = tc }))
                    .GroupBy(x => x.Category.CategoryId)
                    .Select(g => new CategorySpendingInfo
                    {
                        CategoryId = g.Key,
                        CategoryName = categoryNames.GetValueOrDefault(g.Key, "Невідома категорія"),
                        TotalAmount = g.Sum(x => x.Transaction.Amount),
                        Percentage = totalIncomeAmount > 0 ? (double)(g.Sum(x => x.Transaction.Amount) / totalIncomeAmount * 100) : 0,
                        IsBuiltIn = true
                    })
                    .OrderByDescending(c => c.TotalAmount)
                    .ToList();

                return Page();
            }
        }

        private async Task LoadCategoriesAsync(HttpClient client)
        {
            const int maxRetries = 3;
            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    _logger.LogInformation("Fetching categories, attempt {Attempt}", attempt);
                    var response = await client.GetAsync("api/category");
                    if (response.IsSuccessStatusCode)
                    {
                        Categories = await response.Content.ReadFromJsonAsync<List<CategoryDto>>() ?? new List<CategoryDto>();
                        _logger.LogInformation("Fetched {CategoryCount} categories", Categories.Count);
                        return;
                    }
                    _logger.LogWarning("Failed to fetch categories: {StatusCode}, attempt {Attempt}", response.StatusCode, attempt);
                    if (attempt == maxRetries)
                    {
                        ModelError = "Не вдалося завантажити категорії.";
                    }
                    await Task.Delay(500 * attempt);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error fetching categories, attempt {Attempt}", attempt);
                    if (attempt == maxRetries)
                    {
                        ModelError = "Помилка завантаження категорій.";
                    }
                    await Task.Delay(500 * attempt);
                }
            }
        }

        private async Task LoadTransactionsAsync(HttpClient client)
        {
            const int maxRetries = 3;
            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    _logger.LogInformation("Fetching transactions from Monobank API for user {UserId}, attempt {Attempt}", UserInformation.Id, attempt);
                    var response = await client.GetAsync($"api/monobank/transactions/{UserInformation.Id}");
                    if (response.IsSuccessStatusCode)
                    {
                        Transactions = await response.Content.ReadFromJsonAsync<List<TransactionDto>>() ?? new List<TransactionDto>();
                        _logger.LogInformation("Fetched {TransactionCount} transactions for user {UserId}", Transactions.Count, UserInformation.Id);
                        return;
                    }
                    _logger.LogWarning("Failed to fetch transactions from Monobank API for user {UserId}: {StatusCode}, attempt {Attempt}", UserInformation.Id, response.StatusCode);
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("Monobank API error details: {ErrorContent}", errorContent);

                    if (attempt == maxRetries)
                    {
                        _logger.LogInformation("Falling back to database API for transactions for user {UserId}", UserInformation.Id);
                        var fallbackResponse = await client.GetAsync($"api/transaction/user/{UserInformation.Id}");
                        if (fallbackResponse.IsSuccessStatusCode)
                        {
                            Transactions = await fallbackResponse.Content.ReadFromJsonAsync<List<TransactionDto>>() ?? new List<TransactionDto>();
                            _logger.LogInformation("Fetched {TransactionCount} transactions from database API for user {UserId}", Transactions.Count, UserInformation.Id);
                            if (!Transactions.Any())
                            {
                                ModelError = "Не вдалося завантажити транзакції. Використано локальні дані.";
                            }
                        }
                        else
                        {
                            _logger.LogWarning("Failed to fetch transactions from database API for user {UserId}: {StatusCode}", UserInformation.Id, fallbackResponse.StatusCode);
                            ModelError = "Не вдалося завантажити транзакції.";
                        }
                    }
                    await Task.Delay(500 * attempt);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error fetching transactions from Monobank API for user {UserId}, attempt {Attempt}", UserInformation.Id, attempt);
                    if (attempt == maxRetries)
                    {
                        _logger.LogInformation("Falling back to database API for transactions for user {UserId}", UserInformation.Id);
                        try
                        {
                            var fallbackResponse = await client.GetAsync($"api/transaction/user/{UserInformation.Id}");
                            if (fallbackResponse.IsSuccessStatusCode)
                            {
                                Transactions = await fallbackResponse.Content.ReadFromJsonAsync<List<TransactionDto>>() ?? new List<TransactionDto>();
                                _logger.LogInformation("Fetched {TransactionCount} transactions from database API for user {UserId}", Transactions.Count, UserInformation.Id);
                                if (!Transactions.Any())
                                {
                                    ModelError = "Не вдалося завантажити транзакції. Використано локальні дані.";
                                }
                            }
                            else
                            {
                                _logger.LogWarning("Failed to fetch transactions from database API for user {UserId}: {StatusCode}", UserInformation.Id, fallbackResponse.StatusCode);
                                ModelError = "Не вдалося завантажити транзакції.";
                            }
                        }
                        catch (Exception fallbackEx)
                        {
                            _logger.LogError(fallbackEx, "Error fetching transactions from database API for user {UserId}", UserInformation.Id);
                            ModelError = "Помилка завантаження транзакцій.";
                        }
                    }
                    await Task.Delay(500 * attempt);
                }
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
                ModelError = "Виберіть категорію.";
                _logger.LogWarning("Empty category ID provided for transaction {TransactionId}", transactionId);
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
                _logger.LogInformation("Updating category for transaction {TransactionId} to {CategoryId}", transactionId, categoryId);
                var response = await client.PostAsJsonAsync("api/transaction/update-category", request);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Successfully updated category for transaction {TransactionId}", transactionId);
                    return RedirectToPage(new { id = Request.Query["id"], pageNumber = PageNumber });
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                ModelError = $"Помилка оновлення категорії: {errorContent}";
                _logger.LogWarning("Failed to update category for transaction {TransactionId}: {Error}", transactionId, errorContent);
            }
            catch (Exception ex)
            {
                ModelError = $"Помилка: {ex.Message}";
                _logger.LogError(ex, "Error updating transaction category for transaction {TransactionId}", transactionId);
            }

            return RedirectToPage(new { id = Request.Query["id"], pageNumber = PageNumber });
        }

        public async Task<IActionResult> OnPostCreateCategoryAsync()
        {
            var jwt = Request.Cookies["jwt"];
            if (string.IsNullOrEmpty(jwt))
            {
                _logger.LogWarning("No JWT found for category creation");
                return RedirectToPage("/Auth/Login");
            }

            if (string.IsNullOrWhiteSpace(NewCategoryName))
            {
                ModelError = "Назва категорії не може бути порожньою.";
                _logger.LogWarning("Empty category name provided");
                return RedirectToPage(new { id = Request.Query["id"], pageNumber = PageNumber });
            }

            var client = _httpClientFactory.CreateClient("ExpenseTrackerApi");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);

            var createCategoryDto = new CreateCategoryDto
            {
                Name = NewCategoryName,
                ParentCategoryIds = BaseCategoryIds ?? new List<Guid>(),
            };

            try
            {
                _logger.LogInformation("Creating category {CategoryName} with parent IDs {ParentIds}", NewCategoryName, string.Join(",", BaseCategoryIds ?? new List<Guid>()));
                var response = await client.PostAsJsonAsync("api/category", createCategoryDto);
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Successfully created category {CategoryName}", NewCategoryName);
                    await LoadCategoriesAsync(client);
                    return RedirectToPage(new { id = Request.Query["id"], pageNumber = PageNumber });
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                ModelError = $"Помилка створення категорії: {errorContent}";
                _logger.LogWarning("Failed to create category {CategoryName}: {Error}", NewCategoryName, errorContent);
            }
            catch (Exception ex)
            {
                ModelError = $"Помилка: {ex.Message}";
                _logger.LogError(ex, "Error creating category {CategoryName}", NewCategoryName);
            }

            return RedirectToPage(new { id = Request.Query["id"], pageNumber = PageNumber });
        }

        public async Task<IActionResult> OnPostEditCategoryAsync()
        {
            var jwt = Request.Cookies["jwt"];
            if (string.IsNullOrEmpty(jwt))
            {
                _logger.LogWarning("No JWT found for category creation");
                return RedirectToPage("/Auth/Login");
            }

            if (string.IsNullOrWhiteSpace(NewCategoryName))
            {
                CategoryEditError = "Category name is required.";
                return Page();
            }

            if (EditCategoryId == Guid.Empty)
            {
                CategoryEditError = "Invalid category ID.";
                return Page();
            }

            var updateCategoryDto = new UpdateCategoryDto
            {
                Name = NewCategoryName,
                BaseCategoryIds = BaseCategoryIds ?? new List<Guid>()
            };

            var client = _httpClientFactory.CreateClient("ExpenseTrackerApi");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);

            try
            {
                var response = await client.PutAsJsonAsync($"api/category/{EditCategoryId}", updateCategoryDto);
                if (response.IsSuccessStatusCode)
                {
                    NewCategoryName = string.Empty;
                    BaseCategoryIds = new List<Guid>();
                    EditCategoryId = Guid.Empty;
                    return RedirectToPage();
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    CategoryEditError = $"Failed to update category: {errorContent}";
                    return Page();
                }
            }
            catch (HttpRequestException ex)
            {
                CategoryEditError = $"Error connecting to the API: {ex.Message}";
                return Page();
            }
            catch (Exception ex)
            {
                CategoryEditError = $"Unexpected error: {ex.Message}";
                return Page();
            }
        }

        public async Task<IActionResult> OnPostDeleteCategoryAsync(string categoryId)
        {
            var jwt = Request.Cookies["jwt"];
            if (string.IsNullOrEmpty(jwt))
            {
                return RedirectToPage("/Auth/Login");
            }

            if (!Guid.TryParse(categoryId, out var id))
            {
                CategoryDeleteError = "Некоректний ідентифікатор категорії.";
                return Page();
            }

            var client = _httpClientFactory.CreateClient("ExpenseTrackerApi");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);

            try
            {
                var response = await client.DeleteAsync($"api/category/{id}");
                if (response.IsSuccessStatusCode)
                {
                    return RedirectToPage();
                }
                var responseBody = await response.Content.ReadAsStringAsync();
                switch ((int)response.StatusCode)
                {
                    case 404:
                        CategoryDeleteError = "Категорію не знайдено.";
                        break;
                    case 403:
                        CategoryDeleteError = "Неможливо видалити базову категорію або категорію іншого користувача.";
                        break;
                    case 500:
                        CategoryDeleteError = "Базова категорія 'Інше' не знайдена.";
                        break;
                    default:
                        CategoryDeleteError = "Помилка сервера при видаленні категорії.";
                        break;
                }
            }
            catch (Exception ex)
            {
                CategoryDeleteError = "Помилка при з'єднанні з сервером.";
            }

            return Page();
        }
        public async Task<IActionResult> OnPostChangeCredentialsAsync()
        {
            var jwt = Request.Cookies["jwt"];
            if (string.IsNullOrEmpty(jwt))
            {
                return RedirectToPage("/Auth/Login");
            }

            if (!ModelState.IsValid)
            {
                ChangeCredentialsError = "Будь ласка, виправте помилки у формі.";
                return Page();
            }

            var client = _httpClientFactory.CreateClient("ExpenseTrackerApi");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);

            try
            {
                var request = new
                {
                    ChangeCredentialsInput.CurrentPassword,
                    ChangeCredentialsInput.NewLogin,
                    ChangeCredentialsInput.NewPassword,
                    ChangeCredentialsInput.ConfirmNewPassword
                };

                var response = await client.PutAsJsonAsync("api/auth/user", request);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    var error = JsonSerializer.Deserialize<Dictionary<string, string>>(responseContent);
                    ChangeCredentialsError = error?.ContainsKey("message") == true ? error["message"] : "Не вдалося оновити дані.";
                    return Page();
                }

                var successResponse = JsonSerializer.Deserialize<Dictionary<string, object>>(responseContent);
                if (successResponse?.ContainsKey("token") == true)
                {
                    UserInformation.Login = ChangeCredentialsInput.NewLogin;
                }

                ChangeCredentialsInput = new UpdateUserCredentialsRequest();
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                ChangeCredentialsError = $"Помилка: {ex.Message}";
                return Page();
            }
        }
    }
    public record CategorySpendingInfo
    {
        public Guid CategoryId { get; init; }
        public string CategoryName { get; init; }
        public decimal TotalAmount { get; init; }
        public double Percentage { get; init; }
        public bool IsBuiltIn { get; init; }
    }

    public record TransactionDescriptionInfo
    {
        public string Description { get; init; }
        public decimal TotalAmount { get; init; }
        public string TransactionType { get; init; }
    }
}