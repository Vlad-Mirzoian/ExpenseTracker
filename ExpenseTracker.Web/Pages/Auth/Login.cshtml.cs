using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Net.Http.Json;
using System.Text.Json;

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
        [StringLength(25, MinimumLength = 4, ErrorMessage = "Логін має бути від 4 до 25 символів")]
        [RegularExpression(@"^[a-zA-Z0-9_-]+$", ErrorMessage = "Логін може містити лише літери, цифри, підкреслення або дефіси")]
        public string Login { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "Пароль обов'язковий")]
        [StringLength(25, MinimumLength = 8, ErrorMessage = "Пароль має бути від 8 до 25 символів")]
        [RegularExpression(@"^(?=.*[A-Za-z])(?=.*\d).+$", ErrorMessage = "Пароль має містити принаймні одну літеру та одну цифру")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        public string ErrorMessage { get; set; }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            try
            {
                var loginData = new { Login, Password };
                var client = _httpClientFactory.CreateClient("ExpenseTrackerApi");
                var response = await client.PostAsJsonAsync("api/auth/login", loginData);

                if (response.IsSuccessStatusCode)
                {
                    var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();
                    if (authResponse?.Token != null)
                    {
                        Response.Cookies.Append("jwt", authResponse.Token, new CookieOptions
                        {
                            HttpOnly = true,
                            Secure = true,
                            SameSite = SameSiteMode.Strict,
                            Path = "/",
                            Expires = DateTimeOffset.UtcNow.AddDays(1)
                        });
                        return RedirectToPage("/MainPage");
                    }
                    ErrorMessage = "Отримано некоректну відповідь від сервера.";
                    return Page();
                }
                else
                {
                    var errorResponse = await response.Content.ReadFromJsonAsync<ErrorResponse>();
                    ErrorMessage = errorResponse?.Message ?? "Невірний логін або пароль.";
                    ModelState.AddModelError(string.Empty, ErrorMessage);
                    return Page();
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Помилка: {ex.Message}";
                ModelState.AddModelError(string.Empty, ErrorMessage);
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
