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
}
