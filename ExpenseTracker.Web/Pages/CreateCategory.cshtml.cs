using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http.Headers;
using ExpenseTracker.API;

namespace ExpenseTracker.Web.Pages
{
    public class CreateCategoryModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public List<CategoryDto> Categories { get; set; } = new List<CategoryDto>();
        public string ErrorMessage { get; set; }

        public CreateCategoryModel(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var token = Request.Cookies["jwt"];
            if (string.IsNullOrEmpty(token))
            {
                return RedirectToPage("/Auth/Login");
            }

            var client = _httpClientFactory.CreateClient("ExpenseTrackerApi");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var responseCategories = await client.GetAsync("api/category");
            if (responseCategories.IsSuccessStatusCode)
            {
                Categories = await responseCategories.Content.ReadFromJsonAsync<List<CategoryDto>>() ?? new List<CategoryDto>();
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(string name, Guid[] parentCategoryIds)
        {
            var token = Request.Cookies["jwt"];
            if (string.IsNullOrEmpty(token))
            {
                return RedirectToPage("/Auth/Login");
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                ErrorMessage = "Назва категорії є обов'язковою.";
                await LoadCategoriesAsync(token);
                return Page();
            }

            var client = _httpClientFactory.CreateClient("ExpenseTrackerApi");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var request = new
            {
                Name = name,
                ParentCategoryIds = parentCategoryIds ?? Array.Empty<Guid>(),
                IsBuiltIn = false
            };

            try
            {
                var response = await client.PostAsJsonAsync("api/category", request);
                if (response.IsSuccessStatusCode)
                {
                    return RedirectToPage("/MainPage");
                }
                ErrorMessage = "Не вдалося створити категорію.";
            }
            catch (Exception)
            {
                ErrorMessage = "Помилка під час створення категорії.";
            }

            await LoadCategoriesAsync(token);
            return Page();
        }

        private async Task LoadCategoriesAsync(string token)
        {
            var client = _httpClientFactory.CreateClient("ExpenseTrackerApi");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var responseCategories = await client.GetAsync("api/category");
            if (responseCategories.IsSuccessStatusCode)
            {
                Categories = await responseCategories.Content.ReadFromJsonAsync<List<CategoryDto>>() ?? new List<CategoryDto>();
            }
        }
    }
}