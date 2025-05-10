using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace ExpenseTracker.Web.Pages.Auth
{
    public class LoginModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public LoginModel(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [BindProperty]
        [Required(ErrorMessage = "Логін обов'язковий")]
        [MinLength(4, ErrorMessage = "Логін повинен містити щонайменше 4 символи")]
        public string Login { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "Пароль обов'язковий")]
        [MinLength(8, ErrorMessage = "Пароль повинен містити щонайменше 8 символів")]
        public string Password { get; set; }

        public string ErrorMessage { get; set; }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var loginData = new { Login, Password };
            var client = _httpClientFactory.CreateClient("ExpenseTrackerApi");
            var response = await client.PostAsJsonAsync("api/auth/login", loginData);

            if (response.IsSuccessStatusCode)
            {
                var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();
                Response.Cookies.Append("jwt", authResponse.Token, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Path = "/"
                });
                return Redirect("/Index");
            }
            else
            {
                var errorResponse = await response.Content.ReadFromJsonAsync<ErrorResponse>();
                ModelState.AddModelError("", errorResponse?.Message ?? "Помилка входу. Перевірте логін і пароль.");
                return Page();
            }
        }

        public class AuthResponse
        {
            public string Token { get; set; }
        }

        public class ErrorResponse
        {
            public string Message { get; set; }
        }
    }
}
