namespace ExpenseTracker.Data.Model
{
    public class User
    {
        public Guid Id { get; set; } = new Guid();
        public string Name { get; set; }
        public string Token { get; set; }  // API-ключ от Monobank
    }
}
