using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text;
using System.Threading.Tasks;

namespace ExpenseTracker.Web.Pages.Auth
{
    public class RegisterModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public RegisterModel(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [BindProperty]
        public string Login { get; set; }

        [BindProperty]
        public string Password { get; set; }
        [BindProperty]
        public string Token { get; set; }

        public string ErrorMessage { get; set; }

        public async Task<IActionResult> OnPostAsync()
        {
            var client = _httpClientFactory.CreateClient("ExpenseTrackerApi");
            Console.WriteLine("Login: ", Login,"Password: ", Password, "Token: ", Token);
            // Создаем JSON-объект для регистрации
            var registerRequest = new { Login, Password, Token };
            var jsonContent = new StringContent(JsonSerializer.Serialize(registerRequest), Encoding.UTF8, "application/json");

            // Отправляем POST-запрос с JSON
            var response = await client.PostAsync("https://localhost:7151/api/auth/register", jsonContent);

            if (response.IsSuccessStatusCode)
            {
                return RedirectToPage("/Auth/Login");  // Переход на страницу входа после успешной регистрации
            }
            else
            {
                var errorDetails = await response.Content.ReadAsStringAsync();
                ErrorMessage = $"Помилка реєстрації: {errorDetails}";
                return Page();
            }
        }
    }
}
