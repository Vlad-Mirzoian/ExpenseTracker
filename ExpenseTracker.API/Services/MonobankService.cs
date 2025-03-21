using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.Json.Serialization;

public class MonobankService
{
    private readonly HttpClient _httpClient;
    private readonly AppDbContext _context;
    private readonly TransactionService _transactionService;
    private const string BaseUrl = "https://api.monobank.ua";

    public MonobankService(HttpClient httpClient, AppDbContext context, TransactionService transactionService)
    {
        _httpClient = httpClient;
        _context = context;
        _transactionService = transactionService;
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

            if (monobankTransactions == null || !monobankTransactions.Any())
            {
                return new List<Transaction>();
            }

            var transactionDateRange = new DateTimeOffset(fromTimestamp, TimeSpan.Zero).UtcDateTime;

            var existingTransactions = await _context.Transactions
                .Where(tx => tx.UserId == id && tx.Date >= transactionDateRange)
                .ToListAsync();

            var transactionsToSave = new List<Transaction>();

            foreach (var t in monobankTransactions)
            {
                var transactionDate = DateTimeOffset.FromUnixTimeSeconds(t.Time).UtcDateTime;

                var existingTransaction = existingTransactions.FirstOrDefault(tx =>
                    tx.Description == t.Description &&
                    tx.Amount == t.Amount / 100m &&
                    tx.Date == transactionDate);

                if (existingTransaction == null)
                {
                    var transaction = new Transaction
                    {
                        Id = Guid.NewGuid(),
                        UserId = id,
                        Description = t.Description,
                        Amount = t.Amount / 100m,
                        Date = transactionDate,
                        MccCode = t.MccCode
                    };

                    if (transaction.MccCode == 0)
                    {
                        var defaultCategory = await _context.Categories.FirstOrDefaultAsync(c => c.Name == "Інше");
                        transaction.CategoryId = defaultCategory?.Id ?? Guid.Empty;
                    }

                    transactionsToSave.Add(transaction);
                }
            }

            // 5. Сохраняем новые транзакции в БД (если есть)
            if (transactionsToSave.Any())
            {
                await _context.Transactions.AddRangeAsync(transactionsToSave);
                await _context.SaveChangesAsync();
            }

            // 6. Возвращаем объединённый список (существующие + новые)
            return existingTransactions.Concat(transactionsToSave).ToList();
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
    [JsonPropertyName("description")]
    public string Description { get; set; }

    [JsonPropertyName("amount")]
    public int Amount { get; set; } // В копейках

    [JsonPropertyName("time")]
    public long Time { get; set; } // Unix timestamp

    [JsonPropertyName("mcc")]
    public int? MccCode { get; set; } // Теперь правильно!
}
