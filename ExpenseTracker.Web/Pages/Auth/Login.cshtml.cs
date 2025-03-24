using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text;
using System.Threading.Tasks;

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
        public string Login { get; set; }

        [BindProperty]
        public string Password { get; set; }

        public string ErrorMessage { get; set; }

        public async Task<IActionResult> OnPostAsync()
        {
            var loginData = new { Login = Login, Password = Password };
            var client = _httpClientFactory.CreateClient("ExpenseTrackerApi");

            var response = await client.PostAsJsonAsync("api/auth/login", loginData);

            if (response.IsSuccessStatusCode)
            {
                var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();

                // ��������� ����� � �����
                Response.Cookies.Append("jwt", authResponse.Token, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict
                });

                // ����� �������� �� Index
                return Redirect("/Index");
            }
            else
            {
                ModelState.AddModelError("", "������ �����. ��������� ����� � ������.");
                return Page();
            }
        }

        public class AuthResponse
        {
            public string Token { get; set; }
        }

    }
}
