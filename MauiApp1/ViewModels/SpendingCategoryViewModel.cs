﻿using MauiApp1.Models;
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

    public decimal SpendingLimit => _budget * Percentage / 100m;
    public decimal RemainingBudget => SpendingLimit - Expenses.Sum(x => x.Amount);

    private decimal _budget;

    public SpendingCategoryViewModel(SpendingCategory category)
    {
        Category = category;
        AllTransactions = new(Category.Expenses.Cast<Transaction>().Union(Category.Transfers.Cast<Transaction>()));
    }

    public void SetBudget(decimal budget)
    {
        _budget = budget;
        OnPropertyChanged(nameof(SpendingLimit));
        OnPropertyChanged(nameof(RemainingBudget));
    }

    public void AddNewExpense(ExpenseTransaction newExpense)
    {
        if (NewTransactionValue == 0)
            return;

        if (Category.Expenses.Any(x => x.Date > newExpense.Date))
        {
            var index = Category.Expenses.Where(x => x.Date < newExpense.Date).Count();
            Category.Expenses.Insert(index, newExpense);
        }
        else
        {
            Category.Expenses.Add(newExpense);
        }

        UpdateAllTransactions(newExpense, true);

        NewTransactionValue = 0;
        OnPropertyChanged(nameof(RemainingBudget));
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

        OnPropertyChanged(nameof(RemainingBudget));
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
