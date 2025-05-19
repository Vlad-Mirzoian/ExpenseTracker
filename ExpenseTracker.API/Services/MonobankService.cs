using ExpenseTracker.API.Interface;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.Json.Serialization;

public class MonobankService
{
    private readonly HttpClient _httpClient;
    private readonly ITransactionRepository _transactionRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly TransactionCategorizationService _categorizationService;
    private const string BaseUrl = "https://api.monobank.ua";

    public MonobankService(HttpClient httpClient, ITransactionRepository transactionRepository, ICategoryRepository categoryRepository, TransactionCategorizationService categorizationService)
    {
        _httpClient = httpClient;
        _transactionRepository = transactionRepository;
        _categoryRepository = categoryRepository;
        _categorizationService = categorizationService;
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

            var existingTransactions = await _transactionRepository.GetTransactionsByUserAndDateAsync(id, transactionDateRange);

            var transactionsToSave = new List<Transaction>();

            foreach (var t in monobankTransactions)
            {
                var transactionDate = DateTimeOffset.FromUnixTimeSeconds(t.Time).UtcDateTime;

                var existingTransaction = existingTransactions.FirstOrDefault(tx =>
                    Math.Abs(tx.Amount - (t.Amount / 100m)) < 0.01m &&
                    Math.Abs((tx.Date - transactionDate).TotalSeconds) < 1 &&
                    tx.TransactionType == (t.Amount < 0 ? "Expense" : "Income"));

                if (existingTransaction == null)
                {
                    var transaction = new Transaction
                    {
                        Id = Guid.NewGuid(),
                        UserId = id,
                        Description = t.Description,
                        Amount = t.Amount / 100m,
                        Date = transactionDate,
                        MccCode = t.MccCode,
                        TransactionType = t.Amount < 0 ? "Expense" : "Income"
                    };

                    if (transaction.MccCode == 0)
                    {
                        var defaultCategory = await _categoryRepository.GetByNameAsync("Інше");
                        transaction.CategoryId = defaultCategory?.Id ?? Guid.Empty;
                    }
                    else
                    {
                        var defaultCategory = await _categorizationService.CategorizeTransactionAsync(transaction.MccCode.GetValueOrDefault());
                        transaction.CategoryId = defaultCategory;
                    }

                    transactionsToSave.Add(transaction);
                }
            }

            // 5. Сохраняем новые транзакции в БД (если есть)
            if (transactionsToSave.Any())
            {
                foreach(var transactiontoSave in transactionsToSave)
                {
                    await _transactionRepository.AddAsync(transactiontoSave);
                }
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

    [JsonPropertyName("originalMcc")]
    public int? MccCode { get; set; } // Теперь правильно!
}
