using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http.Headers;
using ExpenseTracker.API.Interface;
using ExpenseTracker.Data.Model;
using Microsoft.AspNetCore.Authorization;
namespace ExpenseTracker.Web.Pages
{
    [IgnoreAntiforgeryToken]

    public class IndexModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IUserRepository _userRepository;
        private readonly ITransactionRepository _transactionRepository;

        public User UserInformation { get; set; } = new User();
        public List<Category> Categories { get; set; } = new List<Category>();
        public List<Transaction> Transactions { get; set; } = new List<Transaction>();
        public List<CategorySpendingInfo> CategorySpending { get; set; } = new List<CategorySpendingInfo>();
        public List<CategorySpendingInfo> IncomeByCategory { get; set; } = new List<CategorySpendingInfo>();
        public List<(string Label, decimal TotalAmount)> ExpenseChartData { get; set; } = new List<(string, decimal)>();
        public List<(string Label, decimal TotalAmount)> IncomeChartData { get; set; } = new List<(string, decimal)>();
        public decimal TotalIncome { get; set; }

        public IndexModel(
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
            var token = Request.Cookies["jwt"];
            if (string.IsNullOrEmpty(token))
            {
                return RedirectToPage("/Auth/Login");
            }

            var client = _httpClientFactory.CreateClient("ExpenseTrackerApi");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            try
            {
                // Получение пользователя
                var responseUser = await client.GetAsync("api/auth/me");
                if (!responseUser.IsSuccessStatusCode)
                {
                    return RedirectToPage("/Auth/Login");
                }

                UserInformation = await responseUser.Content.ReadFromJsonAsync<User>() ?? new User();
            }
            catch (Exception ex)
            {
                return RedirectToPage("/Auth/Login");
            }


            var responseCategories = await client.GetAsync("api/category");
            if (responseCategories.IsSuccessStatusCode)
            {
                Categories = await responseCategories.Content.ReadFromJsonAsync<List<Category>>();
            }


            if (id.HasValue)
            {
                // Транзакции по категории
                var response = await client.GetAsync($"api/transaction/category/{id.Value}");
                if (response.IsSuccessStatusCode)
                {
                    Transactions = await response.Content.ReadFromJsonAsync<List<Transaction>>();
                }

                // Chart data for specific category: group by description
                ExpenseChartData = Transactions
                    .Where(t => t.TransactionType == "Expense")
                    .GroupBy(t => t.Description)
                    .Select(g => (Label: g.Key, TotalAmount: g.Sum(t => t.Amount)))
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
                    Transactions = await responseTransactions.Content.ReadFromJsonAsync<List<Transaction>>();
                }

                // Separate expenses and income
                var expenses = Transactions.Where(t => t.TransactionType == "Expense").ToList();
                var income = Transactions.Where(t => t.TransactionType == "Income").ToList();
                TotalIncome = income.Sum(t => t.Amount);

                // Calculate spending per category for expenses
                var totalSpending = expenses.Sum(t => Math.Abs(t.Amount));
                var categoryNames = Categories.ToDictionary(c => c.Id, c => c.Name);
                CategorySpending = expenses
                    .GroupBy(t => categoryNames.ContainsKey(t.CategoryId) ? categoryNames[t.CategoryId] : "Невідома категорія")
                    .Select(g => new CategorySpendingInfo
                    {
                        CategoryName = g.Key,
                        TotalSpending = g.Sum(t => Math.Abs(t.Amount)),
                        Percentage = totalSpending > 0 ? (double)(g.Sum(t => Math.Abs(t.Amount)) / totalSpending * 100) : 0
                    })
                    .OrderByDescending(x => x.TotalSpending)
                    .ToList();

                // Calculate income per category
                var totalIncome = income.Sum(t => t.Amount);
                IncomeByCategory = income
                    .GroupBy(t => categoryNames.ContainsKey(t.CategoryId) ? categoryNames[t.CategoryId] : "Невідома категорія")
                    .Select(g => new CategorySpendingInfo
                    {
                        CategoryName = g.Key,
                        TotalSpending = g.Sum(t => t.Amount),
                        Percentage = totalIncome > 0 ? (double)(g.Sum(t => t.Amount) / totalIncome * 100) : 0
                    })
                    .OrderByDescending(x => x.TotalSpending)
                    .ToList();

                // Chart data for expenses and income
                ExpenseChartData = CategorySpending
                    .Select(x => (Label: x.CategoryName, TotalAmount: x.TotalSpending))
                    .ToList();

                IncomeChartData = IncomeByCategory
                    .Select(x => (Label: x.CategoryName, TotalAmount: x.TotalSpending))
                    .ToList();

                return Page();
            }
        }


        public IActionResult OnPost()
        {
            Console.WriteLine("Вызов OnPostLogout");
            // Удалить токен
            Response.Cookies.Delete("jwt");

            return RedirectToPage("/Auth/Login");
        }

    }

    public record CategorySpendingInfo
    {
        public string CategoryName { get; init; }
        public decimal TotalSpending { get; init; }
        public double Percentage { get; init; }
    }
}