using MauiApp1.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using static MauiApp1.Models.SpendingCategory;

namespace MauiApp1.ViewModels;
public partial class SavingsCategoryViewModel : ObservableObject
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
            OnPropertyChanged(nameof(SavingsGoal));
        }
    }

    public List<ExpenseTransaction> Expenses
    {
        get => Category.Expenses;
        set
        {
            Category.Expenses = value;
            OnPropertyChanged();
        }
    }

    public List<TransferTransaction> Transfers
    {
        get => Category.Transfers;
        set
        {
            Category.Transfers = value;
            OnPropertyChanged();
        }
    }

    public ObservableCollection<Transaction> AllTransactions { get; set; }

    [ObservableProperty]
    decimal newTransactionValue;

    public decimal SavingsGoal => _budget * Percentage / 100m;
    public decimal PotentialSavings => _budget - _otherCategoriesExpensesTotals;
    public bool IsSavingsGoalReached => PotentialSavings >= SavingsGoal;

    private decimal _budget;
    private decimal _otherCategoriesExpensesTotals;

    public SavingsCategoryViewModel(SpendingCategory category)
    {
        Category = category;
        AllTransactions = new(Category.Expenses.Cast<Transaction>().Union(Category.Transfers.Cast<Transaction>()));
    }

    public void SetBudget(decimal budget)
    {
        _budget = budget;
        OnPropertyChanged(nameof(SavingsGoal));
    }

    public void SetOtherCategoreisExpensesTotals(decimal otherCategoriesExpensesTotals)
    {
        _otherCategoriesExpensesTotals = otherCategoriesExpensesTotals;
        OnPropertyChanged(nameof(PotentialSavings));
        OnPropertyChanged(nameof(IsSavingsGoalReached));
    }

    public void AddNewSavingsSpending(DateTime date, string spendingDescription)
    {
        if (NewTransactionValue == 0)
            return;
        ExpenseTransaction newExpense = new(NewTransactionValue, date, "");

        if (Category.Expenses.Any(x => x.Date > date))
        {
            var index = Category.Expenses.Where(x => x.Date < date).Count();
            Category.Expenses.Insert(index, newExpense);
        }
        else
        {
            Category.Expenses.Add(newExpense);
        }

        UpdateAllTransactions(newExpense, true);

        NewTransactionValue = 0;
    }

    public void RemoveTransaction(Transaction transaction)
    {
        if (transaction is ExpenseTransaction expense)
        {
            Category.Expenses.Remove(expense);
        }
        else if (transaction is TransferTransaction transfer)
        {
            Category.Transfers.Remove(transfer);
        }

        UpdateAllTransactions(transaction, false);
    }

    private void UpdateAllTransactions(Transaction transaction, bool add)
    {
        if (add)
        {
            if (AllTransactions.Any(x => x.Date > transaction.Date))
            {
                var index = AllTransactions.Where(x => x.Date < transaction.Date).Count();
                AllTransactions.Insert(index, transaction);
            }

            else
            {
                AllTransactions.Add(transaction);
            }
        }

        else
        {
            AllTransactions.Remove(transaction);
        }
    }
}
