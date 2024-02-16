using MauiApp1.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using static MauiApp1.Models.SpendingCategory;
using System.Diagnostics;

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
    private decimal _percentage;
    public decimal Percentage
    {
        get => _percentage;
        set
        {
            if (_percentage != value)
            {
                _percentage = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(SavingsGoal));

                if (_selectedDate != DateTime.MinValue)
                {
                    PercentageHistory[_selectedDate] = value;
                }
            }

        }
    }
    private Dictionary<DateTime, decimal> PercentageHistory => Category.PercentageHistory;

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
    public ObservableCollection<Transaction> SelectedMonthTransactions { get; set; }

    [ObservableProperty]
    decimal newTransactionValue;

    public decimal TotalSavings
    {
        get
        {
            var incomingTransfers = Transfers.Where(x => x.Destination == Category).Sum(x => x.Amount);
            var outgoingTransfers = Transfers.Where(x => x.Destination != Category).Sum(x => x.Amount);
            var expenses = Expenses.Sum(x => x.Amount);
            return incomingTransfers - (outgoingTransfers + expenses);
        }
    }

    public decimal SelectedMonthSavings
    {
        get
        {
            var incomingTransfers = SelectedMonthTransactions.Where(x => x is TransferTransaction t && t.Destination == Category).Sum(x => x.Amount);
            var outgoingTransfers = SelectedMonthTransactions.Where(x => x is TransferTransaction t && t.Destination != Category).Sum(x => x.Amount);
            var expenses = SelectedMonthTransactions.Where(x => x is ExpenseTransaction).Sum(x => x.Amount);
            return incomingTransfers - (outgoingTransfers + expenses);
        }
    }
    public decimal SavingsGoal => _budget * Percentage / 100m;

    private decimal _budget;
    private DateTime _selectedDate;

    public SavingsCategoryViewModel(SpendingCategory category)
    {
        Category = category;
        AllTransactions = new(Expenses.Cast<Transaction>().Union(Transfers.Cast<Transaction>()));
        SelectedMonthTransactions = new();
    }

    public void SetBudget(decimal budget)
    {
        _budget = budget;
        OnPropertyChanged(nameof(SavingsGoal));
    }

    public void SetAndApplyDate(DateTime date)
    {
        _selectedDate = date;
        GetDatePercentage(_selectedDate);
        GetMonthTransactions(date);
    }

    private void GetDatePercentage(DateTime date)
    {
        var previousOrCurrentDateKeys = PercentageHistory.Keys.Where(x => x <= date);
        Percentage = previousOrCurrentDateKeys.Any() ? PercentageHistory[previousOrCurrentDateKeys.Max()] : 100;
    }

    public void GetMonthTransactions(DateTime date)
    {
        SelectedMonthTransactions.Clear();

        foreach (var transaction in AllTransactions.Where(x => x.Date.Month == date.Month && x.Date.Year == date.Year))
        {
            SelectedMonthTransactions.Add(transaction);
        }
    }

    public void AddNewSavingsExpense(DateTime date, string spendingDescription)
    {
        if (NewTransactionValue == 0) return;

        ExpenseTransaction newExpense = new(NewTransactionValue, date, spendingDescription);
        AddOrInsertTransaction(Expenses, newExpense);
    }

    public void AddNewSavingsTransfer(DateTime date, SpendingCategory destination)
    {
        if (NewTransactionValue == 0) return;

        TransferTransaction newTransfer = new(this.Name, NewTransactionValue, date, destination);
        AddOrInsertTransaction(Transfers, newTransfer);
    }

    private void AddOrInsertTransaction<T>(List<T> transactions, T newTransaction) where T : Transaction
    {
        if (transactions.Any(x => x.Date > newTransaction.Date))
        {
            var index = transactions.Where(x => x.Date < newTransaction.Date).Count();
            transactions.Insert(index, newTransaction);
        }
        else
        {
            transactions.Add(newTransaction);
        }
        UpdateAllAndMonthTransactions(newTransaction, true);

        NewTransactionValue = 0;
    }

    public void RemoveTransaction(Transaction transaction)
    {
        if (transaction is ExpenseTransaction expense)
        {
            Expenses.Remove(expense);
        }
        else if (transaction is TransferTransaction transfer)
        {
            Transfers.Remove(transfer);
        }

        UpdateAllAndMonthTransactions(transaction, false);
    }
    private void UpdateAllAndMonthTransactions(Transaction transaction, bool add)
    {
        if (add)
        {
            SelectedMonthTransactions.Add(transaction);
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
            SelectedMonthTransactions.Remove(transaction);
        }
        OnPropertyChanged(nameof(SelectedMonthSavings));
    }
}
