using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using ExpenseTracker.API.Interface;
using ExpenseTracker.Data.Model;

namespace ExpenseTracker.Web.Pages
{
    public class EditCategoryModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ITransactionRepository _transactionRepository;

        public EditCategoryModel(IHttpClientFactory httpClientFactory, ITransactionRepository transactionRepository)
        {
            _httpClientFactory = httpClientFactory;
            _transactionRepository = transactionRepository;
        }

        [BindProperty]
        public Category Category { get; set; }

        [BindProperty]
        public List<Guid> SelectedTransactionIds { get; set; } = new List<Guid>();

        public List<Transaction> Transactions { get; set; } = new List<Transaction>();
        public User UserInformation { get; set; }
        public async Task<IActionResult> OnGetAsync(Guid? id)
        {
            if (!id.HasValue)
            {
                return NotFound();
            }

            var token = Request.Cookies["jwt"];
            if (string.IsNullOrEmpty(token))
            {
                return RedirectToPage("/Auth/Login");
            }

            var client = _httpClientFactory.CreateClient("ExpenseTrackerApi");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var responseUser = await client.GetAsync("api/auth/me");
            if (responseUser.IsSuccessStatusCode)
            {
                UserInformation = await responseUser.Content.ReadFromJsonAsync<User>();
            }
            else
            {
                return RedirectToPage("/Auth/Login");
            }
            // Получение категории по ID
            var responseCategory = await client.GetAsync($"api/category/{id}");
            if (responseCategory.IsSuccessStatusCode)
            {
                Category = await responseCategory.Content.ReadFromJsonAsync<Category>();
            }
            else
            {
                return NotFound();
            }

            // Получение всех транзакций
            var responseTransactions = await client.GetAsync("api/transaction");
            if (responseTransactions.IsSuccessStatusCode)
            {
                Transactions = await responseTransactions.Content.ReadFromJsonAsync<List<Transaction>>();
            }

            // Определение, какие транзакции уже привязаны к категории
            SelectedTransactionIds = Transactions
                .Where(t => t.CategoryId == id)
                .Select(t => t.Id)
                .ToList();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(Guid id)
        {
            Console.WriteLine($"Received ID: {id}");

            if (Category == null)
            {
                Category = new Category();
            }

            Category.Id = id; // Явно устанавливаем ID
            if (Category.MccCodes == null)
            {
                Category.MccCodes = new int[0]; // Инициализируем пустым массивом, если MccCodes равен null
            }

            var uniqueMccCodes = new HashSet<int>(Category.MccCodes); // Используем HashSet для уникальности

            foreach (var item in SelectedTransactionIds)
            {
                var transact = await _transactionRepository.GetByIdAsync(item);
                if (transact.MccCode.HasValue)
                {
                    uniqueMccCodes.Add(transact.MccCode.Value); // Добавляем только уникальные коды
                }
            }

            // Преобразуем в массив
            Category.MccCodes = uniqueMccCodes.ToArray();
            Console.WriteLine($"ID category after binding: {Category.Id}");

            var token = Request.Cookies["jwt"];
            if (string.IsNullOrEmpty(token))
            {
                return RedirectToPage("/Auth/Login");
            }

            var client = _httpClientFactory.CreateClient("ExpenseTrackerApi");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // Обновление категории
            // Логируем перед отправкой запроса
            var categoryJson = JsonSerializer.Serialize(Category);
            Console.WriteLine($"Sending Category JSON: {categoryJson}");

            var categoryContent = new StringContent(categoryJson, Encoding.UTF8, "application/json");
            var updateCategoryResponse = await client.PutAsync($"api/category/{Category.Id}", categoryContent);

            Console.WriteLine($"Response Status Code: {updateCategoryResponse.StatusCode}");
            Console.WriteLine($"Response Content: {await updateCategoryResponse.Content.ReadAsStringAsync()}");
            if (!updateCategoryResponse.IsSuccessStatusCode)
            {
                ModelState.AddModelError(string.Empty, "Ошибка при обновлении категории.");
                return Page();
            }

            // Обновление привязки транзакций
            if (SelectedTransactionIds != null)
            {
                var updateTransactionRequest = new UpdateTransactionCategoryRequest
                {
                    CategoryId = Category.Id,
                    TransactionIds = SelectedTransactionIds
                };

                var transactionContent = new StringContent(JsonSerializer.Serialize(updateTransactionRequest), Encoding.UTF8, "application/json");
                var updateTransactionResponse = await client.PostAsync("api/transaction/update-category", transactionContent);
                if (!updateTransactionResponse.IsSuccessStatusCode)
                {
                    ModelState.AddModelError(string.Empty, "Ошибка при обновлении транзакций.");
                    return Page();
                }
            }

            return RedirectToPage("/Index");
        }
        public class UpdateTransactionCategoryRequest
        {
            public Guid CategoryId { get; set; }
            public List<Guid> TransactionIds { get; set; }
        }
    }
}
