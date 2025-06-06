﻿using ExpenseTracker.Data.Model;
using System.ComponentModel.DataAnnotations;

public class Transaction
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; }
    [Required]
    public string Description { get; set; }
    public decimal Amount { get; set; }
    public Guid CategoryId { get; set; } // Base (built-in) category
    public Category Category { get; set; }
    private DateTime _date;
    public DateTime Date
    {
        get => _date;
        set => _date = value.Kind == DateTimeKind.Unspecified ? value.ToUniversalTime() : value;
    }
    public int? MccCode { get; set; }
    public string TransactionType { get; set; }
    public bool IsManuallyCategorized { get; set; }
    public List<TransactionCategory> TransactionCategories { get; set; } = new List<TransactionCategory>();
}
