using MauiApp1.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;

namespace MauiApp1.ViewModels;
public partial class SpendingCategoryViewModel : ObservableObject
{
    public SpendingCategory Category { get; }

    public string Name
    {
        get => Category.Name;
        set
        {
            Category.Name = value;
            OnPropertyChanged();
        }
    }

    public string Currency
    {
        get => Category.Currency;
        set
        {
            Category.Currency = value;
            OnPropertyChanged();
        }
    }

    // Not in decimal notation!
    public decimal Percentage
    {
        get => Category.Percentage;
        set
        {
            Category.Percentage = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(SpendingLimit));
            OnPropertyChanged(nameof(RemainingBudget));
        }
    }

    public ObservableCollection<SpendingCategory.Expense> Expenses
    {
        get => Category.Expenses;
        set
        {
            Category.Expenses = value;
            OnPropertyChanged();
        }
    }

    [ObservableProperty]
    decimal newExpenseValue;

    public decimal SpendingLimit => _budget * Percentage / 100m;
    public decimal RemainingBudget => SpendingLimit - Expenses.Sum(x => x.Amount);

    private decimal _budget;

    public SpendingCategoryViewModel(SpendingCategory category)
    {
        Category = category;
    }

    public void SetBudget(decimal budget)
    {
        _budget = budget;
        OnPropertyChanged(nameof(SpendingLimit));
        OnPropertyChanged(nameof(RemainingBudget));
    }

    public void AddNewExpense(DateTime date)
    {
        if (NewExpenseValue == 0)
            return;

        if (Category.Expenses.Any(x => x.Date > date))
        {
            var index = Category.Expenses.Where(x => x.Date < date).Count();
            Category.Expenses.Insert(index, new(NewExpenseValue, date));
        }
        else
        {
            Category.Expenses.Add(new(NewExpenseValue, date));
        }

        NewExpenseValue = 0;
        OnPropertyChanged(nameof(RemainingBudget));
    }

    public void RemoveExpense(SpendingCategory.Expense expense) 
    {
        Category.Expenses.Remove(expense);
        OnPropertyChanged(nameof(RemainingBudget));
    }
}
