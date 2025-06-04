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
        [Required(ErrorMessage = "���� ����'�������")]
        [StringLength(25, MinimumLength = 4, ErrorMessage = "���� �� ���� �� 4 �� 25 �������")]
        [RegularExpression(@"^[a-zA-Z0-9_-]+$", ErrorMessage = "���� ���� ������ ���� �����, �����, ����������� ��� ������")]
        public string Login { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "������ ����'�������")]
        [StringLength(25, MinimumLength = 8, ErrorMessage = "������ �� ���� �� 8 �� 25 �������")]
        [RegularExpression(@"^(?=.*[A-Za-z])(?=.*\d).+$", ErrorMessage = "������ �� ������ �������� ���� ����� �� ���� �����")]
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
                    ErrorMessage = "�������� ���������� ������� �� �������.";
                    return Page();
                }
                else
                {
                    var errorResponse = await response.Content.ReadFromJsonAsync<ErrorResponse>();
                    ErrorMessage = errorResponse?.Message ?? "������� ���� ��� ������.";
                    ModelState.AddModelError(string.Empty, ErrorMessage);
                    return Page();
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"�������: {ex.Message}";
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
