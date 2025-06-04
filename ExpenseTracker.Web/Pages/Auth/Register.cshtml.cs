using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Net.Http.Json;
using System.Text.Json;

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

        [BindProperty]
        [Required(ErrorMessage = "����� ����'�������")]
        public string Token { get; set; }

        public string ErrorMessage { get; set; }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            try
            {
                var registerRequest = new { Login, Password, Token };
                var client = _httpClientFactory.CreateClient("ExpenseTrackerApi");
                var response = await client.PostAsJsonAsync("api/auth/register", registerRequest);

                if (response.IsSuccessStatusCode)
                {
                    return RedirectToPage("/Auth/Login");
                }
                else
                {
                    var errorResponse = await response.Content.ReadFromJsonAsync<ErrorResponse>();
                    ErrorMessage = errorResponse?.Message ?? "������� ���������. �������� ������ ���.";
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

        public class ErrorResponse
        {
            public string Message { get; set; }
        }
    }
}