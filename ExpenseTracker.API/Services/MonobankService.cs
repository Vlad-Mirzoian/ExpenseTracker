using System.Text.Json;
using System.Text.Json.Serialization;

namespace ExpenseTracker.API
{
    public class MonobankService
    {
        private readonly HttpClient _httpClient;
        private readonly ITransactionRepository _transactionRepository;
        private readonly TransactionCategorizationService _categorizationService;
        private readonly ILogger<MonobankService> _logger;
        private const string BaseUrl = "https://api.monobank.ua";

        public MonobankService(
            HttpClient httpClient,
            ITransactionRepository transactionRepository,
            TransactionCategorizationService categorizationService,
            ILogger<MonobankService> logger)
        {
            _httpClient = httpClient;
            _transactionRepository = transactionRepository;
            _categorizationService = categorizationService;
            _logger = logger;
        }

        public async Task<List<Transaction>> GetTransactionsAsync(Guid userId, string token, long fromTimestamp, long toTimestamp)
        {
            _logger.LogInformation("Fetching transactions for user {UserId} from {FromTimestamp} to {ToTimestamp}", userId, fromTimestamp, toTimestamp);

            try
            {
                if (string.IsNullOrEmpty(token))
                {
                    _logger.LogError("Token is empty for user {UserId}", userId);
                    throw new ArgumentException("Monobank token cannot be empty.");
                }

                var request = new HttpRequestMessage(HttpMethod.Get, $"{BaseUrl}/personal/statement/0/{fromTimestamp}/{toTimestamp}");
                request.Headers.Add("X-Token", token);
                _logger.LogDebug("Sending request to {Url}", request.RequestUri);

                var response = await _httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Monobank API error: {StatusCode} - {Error}", response.StatusCode, errorContent);
                    throw new HttpRequestException($"Monobank API request failed: {errorContent}");
                }

                var content = await response.Content.ReadAsStringAsync();
                _logger.LogDebug("Received response: {ContentLength} chars", content.Length);

                var monobankTransactions = JsonSerializer.Deserialize<List<MonobankTransaction>>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (monobankTransactions == null || !monobankTransactions.Any())
                {
                    _logger.LogInformation("No transactions found for user {UserId}", userId);
                    return new List<Transaction>();
                }

                _logger.LogInformation("Deserialized {Count} transactions", monobankTransactions.Count);

                var fromDate = DateTimeOffset.FromUnixTimeSeconds(fromTimestamp).UtcDateTime;
                var toDate = DateTimeOffset.FromUnixTimeSeconds(toTimestamp).UtcDateTime;
                var existingTransactions = await _transactionRepository.GetTransactionsByUserAndDateAsync(userId, fromDate, toDate);
                _logger.LogDebug("Found {Count} existing transactions", existingTransactions.Count);

                var transactionsToSave = new List<Transaction>();

                foreach (var t in monobankTransactions)
                {
                    var transactionDate = DateTimeOffset.FromUnixTimeSeconds(t.Time).UtcDateTime;

                    var existingTransaction = existingTransactions.FirstOrDefault(tx =>
                        Math.Abs(tx.Amount - (t.Amount / 100m)) < 0.01m &&
                        Math.Abs((tx.Date - transactionDate).TotalSeconds) < 1 &&
                        tx.TransactionType == (t.Amount < 0 ? "Expense" : "Income"));

                    if (existingTransaction != null)
                    {
                        _logger.LogDebug("Skipping existing transaction: {Description}", t.Description);
                        continue;
                    }

                    var transaction = new Transaction
                    {
                        Id = Guid.NewGuid(),
                        UserId = userId,
                        Description = t.Description ?? "",
                        Amount = t.Amount / 100m,
                        Date = transactionDate,
                        MccCode = t.MccCode,
                        TransactionType = t.Amount < 0 ? "Expense" : "Income",
                        IsManuallyCategorized = false
                    };

                    transaction.CategoryId = await _categorizationService.CategorizeTransactionAsync(t.MccCode);
                    _logger.LogDebug("Assigned category ID {CategoryId} for transaction {Description}", transaction.CategoryId, transaction.Description);

                    transactionsToSave.Add(transaction);
                }

                if (transactionsToSave.Any())
                {
                    _logger.LogInformation("Saving {Count} new transactions", transactionsToSave.Count);
                    foreach (var transaction in transactionsToSave)
                    {
                        await _transactionRepository.AddAsync(transaction);
                    }
                    _logger.LogInformation("Saved transactions successfully");
                }

                return existingTransactions.Concat(transactionsToSave).ToList();
            }
            catch (ArgumentException ex)
            {
                _logger.LogError(ex, "Validation error");
                throw;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Monobank API error");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error");
                throw;
            }
        }
    }

    public class MonobankTransaction
    {
        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("amount")]
        public int Amount { get; set; }

        [JsonPropertyName("time")]
        public long Time { get; set; }

        [JsonPropertyName("mcc")]
        public int? MccCode { get; set; }
    }
}