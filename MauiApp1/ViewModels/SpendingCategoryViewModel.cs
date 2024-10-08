﻿using MauiApp1.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using MauiApp1.StaticHelpers;
using Newtonsoft.Json;

namespace MauiApp1.ViewModels;
public partial class SpendingCategoryViewModel : ObservableObject
{
    public SpendingCategory Category { get; }

    public string Name
    {
        get => Category.Name;
        set
        {
            Category.Name = value.Trim();

            foreach (var t in Transfers)
            {
                string source = $"Remaining {Name} Balance";
                t.Source = source;
            }
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

                if (_selectedDate != DateTime.MinValue)
                {
                    PercentageHistory[_selectedDate] = value;
                }

                OnPropertyChanged();
                OnPropertyChanged(nameof(MonthSpendingLimit));
                OnPropertyChanged(nameof(RemainingBudget));
                OnPropertyChanged(nameof(CumulativeRemainingBudget));
            }

        }
    }
    public Dictionary<DateTime, decimal> PercentageHistory => Category.PercentageHistory;

    public List<ExpenseTransaction> Expenses
    {
        get => Category.Expenses;
        set
        {
            Category.Expenses = value;
        }
    }

    public List<TransferTransaction> Transfers
    {
        get => Category.Transfers;
        set
        {
            Category.Transfers = value;
        }
    }

    public ObservableCollection<Transaction> AllTransactions { get; set; }
    public ObservableCollection<Transaction> SelectedMonthTransactions { get; set; }
    public ObservableCollection<Transaction> TransactionsToDisplay { get; set; }

    [ObservableProperty]
    decimal newTransactionAmount;
    public decimal MonthSpendingLimit => _budget * Percentage / 100m;
    public decimal CumulativeRemainingBudget => GetCumulativeRemainingBudget(_selectedDate);
    public decimal RemainingBudget => MonthSpendingLimit - SelectedMonthTransactions.Sum(x => x.Amount);

    private decimal _budget;
    private DateTime _selectedDate;
    private IEnumerable<Project> _projects;
    private IEnumerable<CurrencyConversion> _allConversions;

    [JsonConstructor]
    public SpendingCategoryViewModel(SpendingCategory category, IEnumerable<Project> projects, IEnumerable<CurrencyConversion> allConversions)
    {
        _allConversions = allConversions;
        _projects = projects;
        Category = category;
        AllTransactions = new(Category.Expenses.Cast<Transaction>().Union(Category.Transfers.Cast<Transaction>()));
        SelectedMonthTransactions = new();
        TransactionsToDisplay = SelectedMonthTransactions;
    }

    // Constructor to be called when manually creating new Category on UI
    // Sets date as starting point for PercentageHistory
    public SpendingCategoryViewModel(SpendingCategory category, DateTime date, IEnumerable<Project> projects, IEnumerable<CurrencyConversion> allConversions)
    {
        _allConversions = allConversions;
        _projects = projects;
        Category = category;
        PercentageHistory[date] = 0;
        AllTransactions = new();
        SelectedMonthTransactions = new();
        TransactionsToDisplay = SelectedMonthTransactions;
    }

    public void SetBudgetAndDate(decimal budget, DateTime date)
    {
        _budget = budget > 0 ? budget : 0;
        _selectedDate = date;

        Percentage = GetDatePercentage(_selectedDate);
        PopulateSelectedMonthTransactions();
        OnPropertyChanged(nameof(MonthSpendingLimit));
        OnPropertyChanged(nameof(RemainingBudget));
        OnPropertyChanged(nameof(CumulativeRemainingBudget));
    }
    public void SetBudget(decimal budget)
    {
        _budget = budget > 0 ? budget : 0;
        OnPropertyChanged(nameof(MonthSpendingLimit));
        OnPropertyChanged(nameof(RemainingBudget));
        OnPropertyChanged(nameof(CumulativeRemainingBudget));
    }

    public void SetTransactionsCollectionToDisplay(bool all)
    {
        TransactionsToDisplay = all ? AllTransactions : SelectedMonthTransactions;
        OnPropertyChanged(nameof(TransactionsToDisplay));
    }
    public decimal GetDatePercentage(DateTime date)
    {
        var previousOrCurrentDateKeys = PercentageHistory.Keys.Where(x => x <= date);
        return previousOrCurrentDateKeys.Any() ? PercentageHistory[previousOrCurrentDateKeys.Max()] : 0;
    }

    private void PopulateSelectedMonthTransactions()
    {
        SelectedMonthTransactions.Clear();

        foreach (var transaction in AllTransactions.Where(x => x.Date.Month == _selectedDate.Month && x.Date.Year == _selectedDate.Year))
        {
            SelectedMonthTransactions.Add(transaction);
        }
    }

    public IEnumerable<Transaction> GetMonthTransactions(DateTime date)
    {
        return AllTransactions.Where(x => x.Date.Month == date.Month && x.Date.Year == date.Year);
    }

    public void AddNewExpense(string spendingDescription)
    {
        if (NewTransactionAmount == 0) return;

        ExpenseTransaction newExpense = new(NewTransactionAmount, _selectedDate, spendingDescription);
        AddOrInsertTransaction(Expenses, newExpense);
    }

    public void AddNewsTransfer(SavingsCategoryViewModel destinationViewModel)
    {
        if (NewTransactionAmount == 0) return;
        if (NewTransactionAmount > CumulativeRemainingBudget)
        {
            Application.Current.MainPage.DisplayAlert($"Insufficient {Name} Balance", "You cannot transfer more than the remaining cumulative budget balance!", "Ok");
            return;
        }
        string source = $"Remaining {Name} Balance";
        TransferTransaction newTransfer = new(source, NewTransactionAmount, _selectedDate, destinationViewModel.Name);

        // Add the transfer to both the SpendingCategory and the SavingsCategory
        AddOrInsertTransaction(Transfers, newTransfer);
        destinationViewModel.AddIncomingSavingsTransfer(newTransfer);
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
        OnPropertyChanged(nameof(CumulativeRemainingBudget));

        NewTransactionAmount = 0;
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
        OnPropertyChanged(nameof(CumulativeRemainingBudget));
    }

    private void UpdateAllAndMonthTransactions(Transaction transaction, bool add)
    {
        if (add)
        {
            // If transaction date is earlier than cumul calculations starting point
            // Add key to register current month as starting point
            if (transaction.Date <= PercentageHistory.Keys.Min())
            {
                PercentageHistory[transaction.Date] = Percentage;
            }

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
    
    public decimal GetCumulativeRemainingBudget(DateTime date)
    {
        if (PercentageHistory.Keys.Count == 0 || date <= PercentageHistory.Keys.Min())
        {
            return GetRemainingBudget(date);
        }

        var previousMonth = date.AddMonths(-1);
        return GetCumulativeRemainingBudget(previousMonth) + GetRemainingBudget(date);
    }

    private decimal GetRemainingBudget(DateTime date)
    {
        var percentage = GetDatePercentage(date);
        var spendingBudget = ProjectsQuery.CalculateMonthProfitForCurrency(date, Currency, _projects) + GetNetCurrencyConversionsAmount(date);
        spendingBudget = Math.Max(spendingBudget, 0);

        var monthTransactionsAmount = AllTransactions.Where(x => x.Date.Month == date.Month && x.Date.Year == date.Year).Sum(x => x.Amount);

        return (spendingBudget * percentage / 100m) - monthTransactionsAmount;
    }

    private decimal GetNetCurrencyConversionsAmount(DateTime date)
    {
        var outgoing = _allConversions.Where(c => c.Date == date && c.FromCurrency == Currency && !c.IsFromSavings).Sum(c => c.Amount);
        var incoming = _allConversions.Where(c => c.Date == date && c.ToCurrency == Currency && !c.IsToSavings).Sum(c => c.ToAmount);

        return incoming - outgoing;
    }
}
