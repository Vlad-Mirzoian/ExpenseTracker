using System.Net.Http.Headers;
using System.Text.Json;
using ExpenseTracker.Data.Model;

public class MonobankService
{
    private readonly HttpClient _httpClient;
    private const string BaseUrl = "https://api.monobank.ua";

    public MonobankService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<Transaction>> GetTransactionsAsync(Guid id, string token, long fromTimestamp, long toTimestamp)
    {
        try
        {
            if (string.IsNullOrEmpty(token))
            {
                throw new ArgumentException("Токен Monobank не должен быть пустым.");
            }

            var request = new HttpRequestMessage(HttpMethod.Get, $"{BaseUrl}/personal/statement/0/{fromTimestamp}/{toTimestamp}");
            request.Headers.Add("X-Token", token);

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Ошибка Monobank API: {response.StatusCode} - {errorContent}");
            }

            var content = await response.Content.ReadAsStringAsync();
            var monobankTransactions = JsonSerializer.Deserialize<List<MonobankTransaction>>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return monobankTransactions?.Select(t => new Transaction
            {
                Id = Guid.NewGuid(),
                UserId=id,
                Description = t.Description,
                Amount = t.Amount / 100m,
                Date = DateTimeOffset.FromUnixTimeSeconds(t.Time).DateTime
            }).ToList() ?? new List<Transaction>();
        }
        catch (ArgumentException ex)
        {
            throw new Exception($"Ошибка валидации: {ex.Message}");
        }
        catch (HttpRequestException ex)
        {
            throw new Exception($"Ошибка при получении данных из Monobank: {ex.Message}");
        }
        catch (Exception ex)
        {
            throw new Exception($"Непредвиденная ошибка: {ex.Message}");
        }
    }
}

public class MonobankTransaction
{
    public string Description { get; set; }
    public int Amount { get; set; } // В копейках
    public long Time { get; set; } // Unix timestamp
}
