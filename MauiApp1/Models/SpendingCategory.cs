using Newtonsoft.Json;
using System.Collections.ObjectModel;

namespace MauiApp1.Models;
public class SpendingCategory
{
    public string Name { get; set; }
    public string Currency { get; set; }
    public decimal Percentage { get; set; }
    public ObservableCollection<Expense> Expenses { get; set; }

    [JsonConstructor]
    public SpendingCategory(string name, string currency, decimal percentage, List<Expense> expenses)
    {
        Name = name;
        Currency = currency;
        Percentage = percentage;
        // Order by date in case expenses are somehow moved around in JSON file
        Expenses = new(expenses.OrderBy(x => x.Date));
    }

    public SpendingCategory(string name, string currency)
    {
        Name = name;
        Currency = currency;
        Expenses = new();
    }

    public class Expense
    {
        public decimal Amount { get; set; }
        public DateTime Date { get; set; }

        public Expense(decimal amount, DateTime date)
        {
            Amount = amount;
            Date = date;
        }
    }
}
