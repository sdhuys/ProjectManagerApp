using Newtonsoft.Json;

namespace MauiApp1.Models;

public class SpendingCategory
{
    public string Name { get; set; }
    public string Currency { get; set; }
    public Dictionary<DateTime, decimal> PercentageHistory { get; set; }
    public List<ExpenseTransaction> Expenses { get; set; }
    public List<TransferTransaction> Transfers { get; set; }

    [JsonConstructor]
    public SpendingCategory(string name, string currency, Dictionary<DateTime, decimal> percentageHistory, List<ExpenseTransaction> expenses, List<TransferTransaction> transfers) 
    {
        Name = name;
        Currency = currency;
        PercentageHistory = percentageHistory;
        Expenses = expenses;
        Transfers = transfers;
    }

    public SpendingCategory(string name, string currency)
    {
        Name = name;
        Currency = currency;
        Expenses = new();
        Transfers = new();
        PercentageHistory = new();
    }

    public abstract class Transaction
    {
        public decimal Amount { get; set; }
        public DateTime Date { get; set; }
    
        public Transaction(decimal amount, DateTime date)
        {
            Amount = amount;
            Date = date;
        }
    }

    public class ExpenseTransaction : Transaction
    {
        // Currently only Description setting on UI available for Savings Spending
        public string Description { get; set; }
        public ExpenseTransaction(decimal amount, DateTime date, string description) : base(amount, date) 
        {
            Description = description;
        }
    }

    public class TransferTransaction : Transaction
    {
        // Source can be a SpendingCategory, the savings goal portion, or external
        public string Source { get; set; }
        public SpendingCategory Destination { get; set; }
        public TransferTransaction(string source, decimal amount, DateTime date, SpendingCategory destination) : base(amount, date)
        {
            Source = source;
            Destination = destination;
        }
    }
}
