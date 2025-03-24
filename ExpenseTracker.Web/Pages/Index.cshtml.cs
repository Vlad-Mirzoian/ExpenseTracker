using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using Org.BouncyCastle.Asn1.Ocsp;
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

        public User UserInformation { get; set; }
        public List<Category> Categories { get; set; }
        public List<Transaction> Transactions { get; set; }

        public IndexModel(IHttpClientFactory httpClientFactory, IUserRepository userRepository, ITransactionRepository transactionRepository)
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

            // �������� ���������� � ������������
            var responseUser = await client.GetAsync("api/auth/me");
            if (responseUser.IsSuccessStatusCode)
            
            {
                UserInformation = await responseUser.Content.ReadFromJsonAsync<User>();
            }
            else
            {
                return RedirectToPage("/Auth/Login");
            }

            // �������� ��� ���������
            var responseCategories = await client.GetAsync("api/category");
            if (responseCategories.IsSuccessStatusCode)
            {
                Categories = await responseCategories.Content.ReadFromJsonAsync<List<Category>>();
            }

            // ���� �������� ���������, ��������� ���������� �� ���� ���������
            if (id.HasValue)
            {
                Console.WriteLine($"Fetching transactions for categoryId: {id.Value}");
                var response = await client.GetAsync($"api/transaction/category/{id.Value}");

                if (response.IsSuccessStatusCode)
                {
                    Transactions = await response.Content.ReadFromJsonAsync<List<Transaction>>();
                }
                else
                {
                    Console.WriteLine("Error fetching transactions by category");
                }
            }
            else
            {
                // ��������� ��� ���������� ������������, ���� ��������� �� �������
                var responseTransactions = await client.GetAsync($"api/monobank/transactions/{UserInformation.Id}");
                if (responseTransactions.IsSuccessStatusCode)
                {
                    Transactions = await responseTransactions.Content.ReadFromJsonAsync<List<Transaction>>();
                }
            }
            Console.WriteLine($"CategoryId: {id}");
            Console.WriteLine($"Transactions: {Transactions?.Count}");

            return Page();
        }
    }
}
