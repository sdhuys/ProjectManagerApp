using CommunityToolkit.Mvvm.ComponentModel;
using Newtonsoft.Json;

namespace MauiApp1.Models;

public partial class ProjectExpense : ObservableObject
{
    public string Name { get; set; }

    [ObservableProperty]
    decimal amount;
    public DateTime Date { get; set; }
    public bool IsRelative { get; set; }

    public ProjectExpense(string name,  decimal amount, DateTime date)
    {
        Name = name;
        Date = date;
        Amount = amount;
        IsRelative = false;
    }
}

public class ProfitSharingExpense : ProjectExpense
{
    public bool IsPaid { get; set; }
    public decimal RelativeFeeDecimal { get; set; }
    public decimal ExpectedAmount { get; set; } // Set by RelativeExpenseCalculator, but value currently never used/shown
    public ProfitSharingExpense(string name, decimal relFeeDecimal, DateTime date) : base(name, 0, date) // Amount set as 0 on creation, set later by RelativeExpenseCalculator
    {
        RelativeFeeDecimal = relFeeDecimal;
        IsRelative = true;
    }

    // Constructor called in ProjectExpenseJsonConverter for relative expenses
    public ProfitSharingExpense(string name, decimal relFeeDecimal, DateTime date, decimal amount, decimal expectedAmount) : base(name, amount, date)
    {
        RelativeFeeDecimal = relFeeDecimal;
        IsRelative = true;
        ExpectedAmount = expectedAmount;
    }
}