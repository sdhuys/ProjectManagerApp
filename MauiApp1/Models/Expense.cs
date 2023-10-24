using CommunityToolkit.Mvvm.ComponentModel;

namespace MauiApp1.Models;

public partial class Expense : ObservableObject
{
    public string Name { get; set; }

    [ObservableProperty]
    decimal amount;
    public bool IsRelative { get; set; }
    public decimal RelativeFeeDecimal { get; set; }

    public Expense(string name, bool isRelative, decimal value)
    {
        Name = name;
        IsRelative = isRelative;

        if (isRelative)
            RelativeFeeDecimal = value/100;
        else
            Amount = value;
    }
}
