using MauiApp1.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using MauiApp1.StaticHelpers;

namespace MauiApp1.ViewModels;
public partial class SavingsCategoryViewModel : ObservableObject
{
    public SpendingCategory Category { get; }

    // Set to "Savings" on creation inside SpendingOverviewViewModel.CheckForAndCreateMissingSavingsViewModels()
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

                if (_selectedDate != DateTime.MinValue)
                {
                    PercentageHistory[_selectedDate] = value;
                }

                OnPropertyChanged();
                OnPropertyChanged(nameof(CumulativeSavingsGoal));
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

    public List<CurrencyConversion> Conversions { get; set; }

    public ObservableCollection<Transaction> AllTransactions { get; set; }
    public ObservableCollection<Transaction> SelectedMonthTransactions { get; set; }
    public ObservableCollection<Transaction> TransactionsToDisplay { get; set; }

    [ObservableProperty]
    decimal newTransactionAmount;
    [ObservableProperty]
    string selectedTransactionType;
    [ObservableProperty]
    string newTransactionDescription;

    public static string[] TransactionTypes { get; } = new[] { "Expense", "External Deposit" };

    public decimal TotalSavingsUpToSelectedDate
    {
        get
        {
            var incomingTransfers = Transfers.Where(x => x.Destination == Category.Name && x.Date <= _selectedDate).Sum(x => x.Amount);
            var outgoingTransfers = Transfers.Where(x => x.Destination != Category.Name && x.Date <= _selectedDate).Sum(x => x.Amount);
            var incomingConversions = Conversions.Where(x => x.ToCurrency == Currency && x.Date <= _selectedDate).Sum(x => x.Amount);
            var outgoingConversions = Conversions.Where(x => x.FromCurrency == Currency && x.Date <= _selectedDate).Sum(x => x.Amount);
            var expenses = Expenses.Sum(x => x.Amount);

            return (incomingTransfers + incomingConversions) - (outgoingTransfers + outgoingConversions + expenses);
        }
    }

    public decimal SelectedMonthSavings
    {
        get
        {
            var incomingTransfers = SelectedMonthTransactions.Where(x => x is TransferTransaction t && t.Destination == Category.Name).Sum(x => x.Amount);
            var outgoingTransfers = SelectedMonthTransactions.Where(x => x is TransferTransaction t && t.Destination != Category.Name).Sum(x => x.Amount);
            var incomingConversions = SelectedMonthTransactions.Where(x => x is CurrencyConversion c && c.ToCurrency == Currency).Sum(x => x.Amount);
            var outgoingConversions = SelectedMonthTransactions.Where(x => x is CurrencyConversion c && c.FromCurrency == Currency).Sum(x => x.Amount);
            var expenses = SelectedMonthTransactions.Where(x => x is ExpenseTransaction).Sum(x => x.Amount);

            return (incomingTransfers + incomingConversions) - (outgoingTransfers + outgoingConversions + expenses);
        }
    }
    //public decimal SavingsGoal => _budget * Percentage / 100m;

    public decimal SavingsGoal => GetMonthSavingsGoal(_selectedDate);
    public decimal LastMonthSavingsGoalDeficit => _selectedDate == DateTime.MinValue ? 0 : GetMissedSavingsGoalAmount(_selectedDate.AddMonths(-1));
    public decimal CumulativeSavingsGoal => SavingsGoal + LastMonthSavingsGoalDeficit;
    public bool IsLastMonthSavingsGoalReached => SavingsGoal == CumulativeSavingsGoal;
    public bool IsSavingsGoalTransferred => SelectedMonthTransactions.Any(x => x is TransferTransaction t && t.Date == _selectedDate && t.Source == "Savings Goal Portion");
    private DateTime _selectedDate;

    public SavingsCategoryViewModel(SpendingCategory category)
    {
        Category = category;
        AllTransactions = new(Expenses.Cast<Transaction>().Union(Transfers.Cast<Transaction>()));
        SelectedMonthTransactions = new();
        TransactionsToDisplay = SelectedMonthTransactions;
        Conversions = new();

        selectedTransactionType = TransactionTypes[0];

        // Set in case name has somehow been changed in JSON file
        if (Name != "Name") Name = "Savings";
    }

    public void UpdateSavingsGoalUI()
    {
        OnPropertyChanged(nameof(CumulativeSavingsGoal));
        OnPropertyChanged(nameof(LastMonthSavingsGoalDeficit));
    }

    public void SetAndApplyDate(decimal budget, DateTime date)
    {
        _selectedDate = date;
        Percentage = GetDatePercentage(_selectedDate);
        PopulateSelectedMonthTransactions(date);

        UpdateSavingsGoalUI();
        OnPropertyChanged(nameof(SelectedMonthSavings));
        OnPropertyChanged(nameof(TotalSavingsUpToSelectedDate));
        OnPropertyChanged(nameof(IsSavingsGoalTransferred));
        OnPropertyChanged(nameof(IsLastMonthSavingsGoalReached));
    }

    public void SetTransactionsCollectionToDisplay(bool all)
    {
        TransactionsToDisplay = all ? AllTransactions : SelectedMonthTransactions;
        OnPropertyChanged(nameof(TransactionsToDisplay));
    }
    private decimal GetDatePercentage(DateTime date)
    {
        var previousOrCurrentDateKeys = PercentageHistory.Keys.Where(x => x <= date);
        return previousOrCurrentDateKeys.Any() ? PercentageHistory[previousOrCurrentDateKeys.Max()] : 100;
    }
    public IEnumerable<Transaction> GetMonthTransactions(DateTime date)
    {
        return AllTransactions.Where(x => x.Date.Month == date.Month && x.Date.Year == date.Year);
    }

    public void PopulateSelectedMonthTransactions(DateTime date)
    {
        SelectedMonthTransactions.Clear();

        foreach (var transaction in AllTransactions.Where(x => x.Date.Month == date.Month && x.Date.Year == date.Year))
        {
            SelectedMonthTransactions.Add(transaction);
        }
    }

    // Called when ExpensesAreFinalised and total expenses exceed budget
    // => spendings have to come out of savings
    public void AddIncomingSavingsExpense(ExpenseTransaction newExpense)
    {
        AddOrInsertTransaction(Expenses, newExpense);
    }

    // Called when remaining balance from SpendingCategory is transferred to savings
    // Called when ExpensesAreFinalised and SavingsGoal is transferred to savings 
    public void AddIncomingSavingsTransfer(TransferTransaction newTransfer)
    {
        AddOrInsertTransaction(Transfers, newTransfer);
        OnPropertyChanged(nameof(IsSavingsGoalTransferred));
    }

    public void AddIncomingSavingsConversion(CurrencyConversion newConversion)
    {
        AddOrInsertTransaction(Conversions, newConversion);
    }

    public void AddNewTransaction()
    {
        if (SelectedTransactionType == TransactionTypes[0])
        {
            AddNewSavingsExpense();
        }

        else
        {
            AddNewSavingsTransfer();
        }
    }
    public void AddNewSavingsTransfer()
    {
        if (NewTransactionAmount == 0) return;

        var newTransfer = new TransferTransaction($"External Transfer: {NewTransactionDescription}", NewTransactionAmount, _selectedDate, Name);
        AddOrInsertTransaction(Transfers, newTransfer);
    }
    public void AddNewSavingsExpense()
    {
        if (NewTransactionAmount == 0) return;

        ExpenseTransaction newExpense = new(NewTransactionAmount, _selectedDate, $"External Expense: {NewTransactionDescription}");
        AddOrInsertTransaction(Expenses, newExpense);
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

        NewTransactionAmount = 0;
        NewTransactionDescription = String.Empty;
    }

    public void RemoveTransaction(Transaction transaction)
    {
        if (transaction == null) return;

        if (transaction is ExpenseTransaction expense)
        {
            Expenses.Remove(expense);
        }
        else if (transaction is TransferTransaction transfer)
        {
            Transfers.Remove(transfer);
        }
        else if (transaction is CurrencyConversion conversion)
        {
            Conversions.Remove(conversion);
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

            if (transaction is TransferTransaction t && t.Source == "Savings Goal Portion")
            {
                OnPropertyChanged(nameof(IsSavingsGoalTransferred));
            }
        }
        OnPropertyChanged(nameof(CumulativeSavingsGoal));
        OnPropertyChanged(nameof(SelectedMonthSavings));
        OnPropertyChanged(nameof(TotalSavingsUpToSelectedDate));
    }

    public decimal CalculateCumulSavingsGoal(DateTime date)
    {
        if (date == DateTime.MinValue) return 0;

        var previousMonth = date.AddMonths(-1);

        return GetMonthSavingsGoal(date) + GetMissedSavingsGoalAmount(previousMonth);
    }

    private decimal GetMissedSavingsGoalAmount(DateTime date)
    {
        if (PercentageHistory.Keys.Count == 0 || date < PercentageHistory.Keys.Min()) return 0;

        var transferredToSavingsAmount = AllTransactions.Where(x => x is TransferTransaction t && t.Date == date && t.Source == "Savings Goal Portion").Sum(x => x.Amount);
        var fromSavingsAmount = AllTransactions.Where(x => x is ExpenseTransaction e && e.Date == date && e.Description == "Spendings Overdraft Coverage").Sum(x => x.Amount);

        return CalculateCumulSavingsGoal(date) - transferredToSavingsAmount + fromSavingsAmount;
    }

    private decimal GetMonthSavingsGoal(DateTime date)
    {
        var percentage = GetDatePercentage(date);
        var spendingBudget = ProjectsQuery.CalculateMonthProfitForCurrency(date, Currency) + GetNetCurrencyConversionsAmount(date);
        spendingBudget = Math.Max(spendingBudget, 0);

        return (spendingBudget * percentage / 100m);
    }

    private decimal GetNetCurrencyConversionsAmount(DateTime date)
    {
        var outgoing = CurrencyConversionManager.AllConversions.Where(c => c.Date == date && c.FromCurrency == Currency && !c.IsFromSavings).Sum(c => c.Amount);
        var incoming = CurrencyConversionManager.AllConversions.Where(c => c.Date == date && c.ToCurrency == Currency && !c.IsToSavings).Sum(c => c.ToAmount);

        return incoming - outgoing;
    }
}
