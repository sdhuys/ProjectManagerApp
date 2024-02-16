using MauiApp1.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using static MauiApp1.Models.SpendingCategory;

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
                OnPropertyChanged(nameof(MonthSpendingLimit));
                OnPropertyChanged(nameof(RemainingBudget));

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
    private decimal previousCumulativeBudget = 0;
    public decimal MonthSpendingLimit => _budget * Percentage / 100m;
    public decimal RemainingBudget => previousCumulativeBudget + MonthSpendingLimit - SelectedMonthTransactions.Sum(x => x.Amount);

    private decimal _budget;
    private DateTime _selectedDate;

    public SpendingCategoryViewModel(SpendingCategory category)
    {
        Category = category;
        AllTransactions = new(Category.Expenses.Cast<Transaction>().Union(Category.Transfers.Cast<Transaction>()));
        SelectedMonthTransactions = new();
    }

    public void SetBudget(decimal budget)
    {
        _budget = budget;
        OnPropertyChanged(nameof(MonthSpendingLimit));
        OnPropertyChanged(nameof(RemainingBudget));
    }

    public void SetAndApplyDate(DateTime date)
    {
        _selectedDate = date;
        GetDatePercentage(_selectedDate);
        GetMonthTransactions(_selectedDate);
        OnPropertyChanged(nameof(RemainingBudget));
    }

    private void GetDatePercentage(DateTime date)
    {
        var previousOrCurrentDateKeys = PercentageHistory.Keys.Where(x => x <= date);
        Percentage = previousOrCurrentDateKeys.Any() ? PercentageHistory[previousOrCurrentDateKeys.Max()] : 0;
    }

    private void GetMonthTransactions(DateTime date)
    {
        SelectedMonthTransactions.Clear();

        foreach (var transaction in AllTransactions.Where(x => x.Date.Month == date.Month && x.Date.Year == date.Year))
        {
            SelectedMonthTransactions.Add(transaction);
        }
    }

    public void AddNewExpense(DateTime date, string spendingDescription)
    {
        if (NewTransactionValue == 0) return;

        ExpenseTransaction newExpense = new(NewTransactionValue, date, spendingDescription);
        AddOrInsertTransaction(Expenses, newExpense);
    }

    public void AddNewsTransfer(DateTime date, SpendingCategory destination)
    {
        if (NewTransactionValue == 0) return;
        if (NewTransactionValue > RemainingBudget)
        {
            Application.Current.MainPage.DisplayAlert("Insufficient Balance", "You cannot transfer more than the remaining budget balance!", "Ok");
            return;
        }
        TransferTransaction newTransfer = new(Name, NewTransactionValue, date, destination);
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
        OnPropertyChanged(nameof(RemainingBudget));

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

        UpdateAllAndMonthTransactions(transaction, false);
        OnPropertyChanged(nameof(RemainingBudget));
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
    }
}
