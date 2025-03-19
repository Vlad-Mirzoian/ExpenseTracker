using ExpenseTracker.Data.Model;

public class Transaction
{
    public Guid Id { get; set; } = new Guid();
    public Guid UserId { get; set; }
    public User User { get; set; }
    public string Description { get; set; }
    public decimal Amount { get; set; }
    public Guid CategoryId { get; set; }
    public Category Category { get; set; }
    public DateTime Date { get; set; }
}