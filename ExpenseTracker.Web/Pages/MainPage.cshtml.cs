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

        public User UserInformation { get; set; } = new User();
        public List<CategoryDto> Categories { get; set; } = new List<CategoryDto>();
        public List<TransactionDto> Transactions { get; set; } = new List<TransactionDto>();
        public List<CategorySpendingInfo2> CategorySpending { get; set; } = new List<CategorySpendingInfo2>();
        public List<CategorySpendingInfo2> IncomeByCategory { get; set; } = new List<CategorySpendingInfo2>();
        public List<(string Label, decimal TotalAmount)> ExpenseChartData { get; set; } = new List<(string, decimal)>();
        public List<(string Label, decimal TotalAmount)> IncomeChartData { get; set; } = new List<(string, decimal)>();
        public decimal TotalIncome { get; set; }
        public string SyncError { get; set; }
        public string UpdateCategoryError { get; set; }

        public MainPageModel(
            IHttpClientFactory httpClientFactory,
            IUserRepository userRepository,
            ITransactionRepository transactionRepository)
        {
            _httpClientFactory = httpClientFactory;
            _userRepository = userRepository;
            _transactionRepository = transactionRepository;
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
                    return RedirectToPage("/Auth/Login");
                }

                UserInformation = await responseUser.Content.ReadFromJsonAsync<User>() ?? new User();

                var user = await _userRepository.GetByIdAsync(UserInformation.Id);
                if (user == null)
                {
                    return RedirectToPage("/Auth/Login");
                }
            }
            catch (Exception ex)
            {
                return RedirectToPage("/Auth/Login");
            }

            var responseCategories = await client.GetAsync("api/category");
            if (responseCategories.IsSuccessStatusCode)
            {
                Categories = await responseCategories.Content.ReadFromJsonAsync<List<CategoryDto>>() ?? new List<CategoryDto>();
            }

            if (id.HasValue)
            {
                var response = await client.GetAsync($"api/transaction/category/{id.Value}");
                if (response.IsSuccessStatusCode)
                {
                    Transactions = await response.Content.ReadFromJsonAsync<List<TransactionDto>>() ?? new List<TransactionDto>();
                }

                ExpenseChartData = Transactions
                    .Where(t => t.TransactionType == "Expense")
                    .GroupBy(t => t.Description)
                    .Select(g => (Label: (string)g.Key, TotalAmount: g.Sum(t => t.Amount)))
                    .ToList();

                IncomeChartData = Transactions
                    .Where(t => t.TransactionType == "Income")
                    .GroupBy(t => t.Description)
                    .Select(g => (gLabel: (string)g.Key, TotalAmount: g.Sum(t => t.Amount)))
                    .ToList();

                return Page();
            }
            else
            {
                var responseTransactions = await client.GetAsync($"api/monobank/transactions/{UserInformation.Id}");
                if (responseTransactions.IsSuccessStatusCode)
                {
                    Transactions = await responseTransactions.Content.ReadFromJsonAsync<List<TransactionDto>>() ?? new List<TransactionDto>();
                }

                var expenses = Transactions.Where(t => t.TransactionType == "Expense").ToList();
                var income = Transactions.Where(t => t.TransactionType == "Income").ToList();
                TotalIncome = income.Sum(t => t.Amount);

                var totalSpending = expenses.Sum(t => Math.Abs(t.Amount));
                var categoryNames = Categories.ToDictionary(c => c.Id, c => c.Name);
                CategorySpending = expenses
                    .GroupBy(t => categoryNames.ContainsKey(t.CategoryId) ? categoryNames[t.CategoryId] : "Невідома категорія")
                    .Select(g => new CategorySpendingInfo2
                    {
                        CategoryName = g.Key,
                        TotalSpending = g.Sum(t => Math.Abs(t.Amount)),
                        Percentage = totalSpending > 0 ? (double)(g.Sum(t => Math.Abs(t.Amount)) / totalSpending * 100) : 0
                    })
                    .OrderByDescending(x => x.TotalSpending)
                    .ToList();

                var totalIncome = income.Sum(t => t.Amount);
                IncomeByCategory = income
                    .GroupBy(t => categoryNames.ContainsKey(t.CategoryId) ? categoryNames[t.CategoryId] : "Невідома категорія")
                    .Select(g => new CategorySpendingInfo2
                    {
                        CategoryName = g.Key,
                        TotalSpending = g.Sum(t => t.Amount),
                        Percentage = totalIncome > 0 ? (double)(g.Sum(t => t.Amount) / totalIncome * 100) : 0
                    })
                    .OrderByDescending(x => x.TotalSpending)
                    .ToList();

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
                return RedirectToPage(new { id = Request.Query["id"] });
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
                    return RedirectToPage(new { id = Request.Query["id"] });
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                UpdateCategoryError = $"Помилка: {errorContent}";
            }
            catch (Exception ex)
            {
                UpdateCategoryError = $"Помилка: {ex.Message}";
            }

            return RedirectToPage(new { id = Request.Query["id"] });
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