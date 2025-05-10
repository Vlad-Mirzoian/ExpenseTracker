using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http.Headers;
using ExpenseTracker.API.Interface;
using ExpenseTracker.Data.Model;
namespace ExpenseTracker.Web.Pages
{
    public class IndexModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IUserRepository _userRepository;
        private readonly ITransactionRepository _transactionRepository;

        public User UserInformation { get; set; } = new User();
        public List<Category> Categories { get; set; } = new List<Category>();
        public List<Transaction> Transactions { get; set; } = new List<Transaction>();
        public List<CategorySpendingInfo> CategorySpending { get; set; } = new List<CategorySpendingInfo>();
        public List<(string Label, decimal TotalAmount)> ChartData { get; set; } = new List<(string, decimal)>();

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

                    ChartData = Transactions
                        .GroupBy(t => t.Description)
                        .Select(g => (Label: g.Key, TotalAmount: g.Sum(t => t.Amount)))
                        .ToList();
                }
                else
                {
                    // Все транзакции
                    var responseTransactions = await client.GetAsync($"api/monobank/transactions/{UserInformation.Id}");
                    if (responseTransactions.IsSuccessStatusCode)
                    {
                        Transactions = await responseTransactions.Content.ReadFromJsonAsync<List<Transaction>>();
                    }

                    var totalSpending = Transactions.Sum(t => t.Amount);
                    var categoryNames = Categories.ToDictionary(c => c.Id, c => c.Name);
                    CategorySpending = Transactions
                        .GroupBy(t => categoryNames.ContainsKey(t.CategoryId) ? categoryNames[t.CategoryId] : "Невідома категорія")
                        .Select(g => new CategorySpendingInfo
                        {
                            CategoryName = g.Key,
                            TotalSpending = g.Sum(t => t.Amount),
                            Percentage = totalSpending > 0 ? (double)(g.Sum(t => t.Amount) / totalSpending * 100) : 0
                        })
                        .OrderByDescending(x => x.TotalSpending)
                        .ToList();

                    ChartData = CategorySpending
                        .Select(x => (Label: x.CategoryName, TotalAmount: x.TotalSpending))
                        .ToList();
                }

            return Page();
        }


        public IActionResult OnPostlogout()
        {
            throw new Exception("Проверка: OnPostLogout вызывается");

            // Удаление cookie jwt с флагом HttpOnly, Secure, и другими параметрами
            Response.Cookies.Delete("jwt", new CookieOptions
            {
                HttpOnly = true,          // Безопасность — доступна только через HTTP
                Secure = true,            // Только для HTTPS
                SameSite = SameSiteMode.Strict, // Защита от межсайтовых запросов
                Path = "/",              // Доступна для всего сайта
            });

            // Редирект на страницу логина
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