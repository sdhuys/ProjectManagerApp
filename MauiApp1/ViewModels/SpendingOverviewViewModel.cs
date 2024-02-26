﻿using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MauiApp1.Models;
using MauiApp1.StaticHelpers;
using System.Collections.ObjectModel;

namespace MauiApp1.ViewModels;

public partial class SpendingOverviewViewModel : ObservableObject
{
    [ObservableProperty]
    bool showAllTransactions;
    public string[] ViewOptions { get; }

    [ObservableProperty]
    string selectedViewOption;

    [ObservableProperty]
    DateTime selectedDate = new (DateTime.Today.Year, DateTime.Today.Month, 1);

    public DateTime MinDate
    {
        get
        {
            DateTime today = DateTime.Today;

            DateTime minProjectDate = ProjectManager.AllProjects
                .SelectMany(project => project.Expenses.Select(e => e.Date).Union(project.Payments.Select(p => p.Date)))
                .DefaultIfEmpty(today)
                .Min();

            DateTime minCatsDictDate = SelectedCurrencySpendingCategoryViewModels
                .SelectMany(x => x.PercentageHistory?.Keys ?? Enumerable.Empty<DateTime>())
                .DefaultIfEmpty(today)
                .Min();

            DateTime minSavingsDictDate = SelectedSavingsCategoryViewModel?.PercentageHistory?.Keys?.Min() ?? today;

            DateTime minDate = new[]
            {
                minProjectDate,
                minCatsDictDate,
                minSavingsDictDate
            }.Min();

            return new DateTime(minDate.Year, minDate.Month, 1);
        }
    }

    [ObservableProperty]
    string selectedCurrency;

    private List<string> _currencyList;
    public List<string> CurrencyList => _currencyList;
    public List<string> NonSelectedCurrencies => CurrencyList?.Where(c => c != SelectedCurrency).ToList();

    public List<SpendingCategoryViewModel> SpendingCategoryViewModels { get; set; }
    public ObservableCollection<SpendingCategoryViewModel> SelectedCurrencySpendingCategoryViewModels { get; set; }
    public List<SavingsCategoryViewModel> SavingsCategoryViewModels { get; set; }
    public SavingsCategoryViewModel SelectedSavingsCategoryViewModel => SavingsCategoryViewModels.FirstOrDefault(cat => cat.Currency == SelectedCurrency);

    public List<CurrencyConversion> CurrencyConversions { get; set; }
    private IEnumerable<CurrencyConversion> ToSelectedCurrencySavingsConversions => !ShowAllTransactions ?
                                                                                    CurrencyConversions.Where(c => c.ToCurrency == SelectedCurrency && c.IsToSavings
                                                                                    && c.Date.Month == SelectedDate.Month && c.Date.Year == SelectedDate.Year)
                                                                                    : CurrencyConversions.Where(c => c.ToCurrency == SelectedCurrency && c.IsToSavings);
    private IEnumerable<CurrencyConversion> ToSelectedCurrencyNonSavingsConversions => !ShowAllTransactions ?
                                                                                       CurrencyConversions.Where(c => c.ToCurrency == SelectedCurrency && !c.IsToSavings
                                                                                       && c.Date.Month == SelectedDate.Month && c.Date.Year == SelectedDate.Year)
                                                                                       : CurrencyConversions.Where(c => c.ToCurrency == SelectedCurrency && !c.IsToSavings);
    private IEnumerable<CurrencyConversion> FromSelectedCurrencySavingsConversions => !ShowAllTransactions ?
                                                                                      CurrencyConversions.Where(c => c.FromCurrency == SelectedCurrency && c.IsFromSavings
                                                                                      && c.Date.Month == SelectedDate.Month && c.Date.Year == SelectedDate.Year)
                                                                                      : CurrencyConversions.Where(c => c.FromCurrency == SelectedCurrency && c.IsFromSavings);
    private IEnumerable<CurrencyConversion> FromSelectedCurrencyNonSavingsConversions => !ShowAllTransactions ?
                                                                                         CurrencyConversions.Where(c => c.FromCurrency == SelectedCurrency && !c.IsFromSavings
                                                                                         && c.Date.Month == SelectedDate.Month && c.Date.Year == SelectedDate.Year)
                                                                                         : CurrencyConversions.Where(c => c.FromCurrency == SelectedCurrency && !c.IsFromSavings);

    [ObservableProperty]
    ObservableCollection<CurrencyConversion> selectedCurrencyConversions;
    public decimal SelectedCurrencyNetNonSavingsConversions => GetNetNonSavingsConversionsAmount(SelectedDate);
    public decimal ActualProfitForCurrency => ProjectsQuery.CalculateMonthProfitForCurrency(SelectedDate, SelectedCurrency);
    public decimal TotalSpendingBudget => ActualProfitForCurrency + SelectedCurrencyNetNonSavingsConversions;
    public decimal TotalCumulRemainingBudget => SelectedCurrencySpendingCategoryViewModels.Sum(cat => cat.CumulativeRemainingBudget);
    public SavingsGoalReachedStatus IsSavingsGoalReached
    {
        get
        {
            var cumulSavingsGoal = SelectedSavingsCategoryViewModel?.CumulativeSavingsGoal;

            if (TotalCumulRemainingBudget >= 0 && cumulSavingsGoal != 0)
                return SavingsGoalReachedStatus.Fully;
            else if (TotalCumulRemainingBudget >= 0 && cumulSavingsGoal == 0)
                return SavingsGoalReachedStatus.Zero;
            else if (Math.Abs(TotalCumulRemainingBudget) < cumulSavingsGoal)
                return SavingsGoalReachedStatus.Partly;
            else if (Math.Abs(TotalCumulRemainingBudget) == cumulSavingsGoal)
                return SavingsGoalReachedStatus.Zero;
            else
                return SavingsGoalReachedStatus.Negative;

        }
    }

    public bool CanFinaliseExpenses
    {
        get
        {
            if (SelectedDate == MinDate) return true;

            var lastMonth = SelectedDate.AddMonths(-1);
            var key = GenerateFinalisedMonthsDictKey(lastMonth);

            return _finalisedMonthsDictionary.TryGetValue(key, out bool result) && result && !EditMode;
        }
    }


    // Dictionary containing keys in "CURRENCY:MM/YY" format, values representing whether or not that months' accounting is finalized
    private Dictionary<string, bool> _finalisedMonthsDictionary;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddCurrencyConversionCommand))]
    bool expensesAreFinalised;

    private bool _blockOnExpensesFinalisedChanged;

    [ObservableProperty]
    bool editMode;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddCurrencyConversionCommand))]
    bool isFromSavingsConversion;

    [ObservableProperty]
    bool isToSavingsConversion;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddCurrencyConversionCommand))]
    string newToCurrencyEntry;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddCurrencyConversionCommand))]
    decimal newToAmountEntry;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddCurrencyConversionCommand))]
    decimal newFromAmountEntry;

    public SpendingOverviewViewModel()
    {
        var (spendings, savings, dict) = SpendingOverviewDataManager.LoadFromJson();
        SpendingCategoryViewModels = spendings.Select(x => new SpendingCategoryViewModel(x)).ToList();
        SavingsCategoryViewModels = savings.Select(x => new SavingsCategoryViewModel(x)).ToList();
        _finalisedMonthsDictionary = dict;
        
        CurrencyConversions = CurrencyConversionManager.LoadFromJson();
        SelectedCurrencyConversions = new();
        SelectedCurrencySpendingCategoryViewModels = new();

        _currencyList = ProjectManager.AllProjects.Select(p => p.Currency).Distinct().ToList();
        ViewOptions = new[] { "Spending Categories", "Savings" };
        SelectedViewOption = ViewOptions[0];

        CheckForAndCreateMissingSavingsViewModels();
        AddSavingsConversionsToSavingsViewModels();
    }
    private void AddSavingsConversionsToSavingsViewModels()
    {
        foreach (var conv in CurrencyConversions)
        {
            if (conv.IsFromSavings)
            {
                var fromSavingsVM = SavingsCategoryViewModels.FirstOrDefault(x => x.Currency == conv.FromCurrency);
                fromSavingsVM?.AddIncomingSavingsConversion(conv);
            }
            if (conv.IsToSavings)
            {
                var toSavingsVM = SavingsCategoryViewModels.FirstOrDefault(x => x.Currency == conv.ToCurrency);
                toSavingsVM?.AddIncomingSavingsConversion(conv);
            }
        }
    }

    public void OnAppearing()
    {
        _currencyList = ProjectManager.AllProjects.Select(p => p.Currency).Distinct().ToList();
        OnPropertyChanged(nameof(CurrencyList));
        OnPropertyChanged(nameof(MinDate));

        CheckForAndCreateMissingSavingsViewModels();

        // Set selected currency to the one with highest payments received amount
        SelectedCurrency = CurrencyList.OrderByDescending(c => ProjectManager.AllProjects.Where(p => p.Currency == c).Count()).FirstOrDefault();
    }

    partial void OnSelectedCurrencyChanged(string value)
    {
        if (value == null) return;

        PopulateSelectedCurrencyConversions();
        PopulateSelectedCurrencySpendingCategories();
        OnPropertyChanged(nameof(SelectedSavingsCategoryViewModel));
        OnPropertyChanged(nameof(TotalSpendingBudget));
        OnPropertyChanged(nameof(IsSavingsGoalReached));
        SetCategoriesBudgetandDate();
        OnPropertyChanged(nameof(NonSelectedCurrencies));
        SetExpensesAreFinalisedFromDictionary();
        CheckCurrentMonthPercentages();
        OnPropertyChanged(nameof(CanFinaliseExpenses));
        SelectedDate = GetEarliestUnfinalisedMonth();
    }

    partial void OnSelectedDateChanged(DateTime oldValue, DateTime newValue)
    {
        SelectedDate = new DateTime(newValue.Year, newValue.Month, 1);

        if (newValue.Month != oldValue.Month || newValue.Year != oldValue.Year)
        {
            SetCategoriesBudgetandDate();
            PopulateSelectedCurrencyConversions();
            OnPropertyChanged(nameof(TotalSpendingBudget));
            SetExpensesAreFinalisedFromDictionary();
            CheckCurrentMonthPercentages();
            OnPropertyChanged(nameof(CanFinaliseExpenses));
        }
    }

    partial void OnEditModeChanged(bool value)
    {
        OnPropertyChanged(nameof(CanFinaliseExpenses));
        if (!value)
        {
            CheckCategoryNamesAreUnique();
            CheckCurrentMonthPercentages();
        }
    }

    partial void OnShowAllTransactionsChanged(bool value)
    {
        foreach (var cat in SelectedCurrencySpendingCategoryViewModels)
        {
            cat.SetTransactionsCollectionToDisplay(value);
        }
        SelectedSavingsCategoryViewModel.SetTransactionsCollectionToDisplay(value);
        PopulateSelectedCurrencyConversions();
    }

    // When expenses are set to Finalised, automatically calcualate and add the available portion of savings goal to transfer to savings
    // If expenses exceed budget, add Expense to savings to cover for the overdraft
    //
    // When expenses are set back to Unfinalised, remove automatically added transfer or expenses
    //
    // Immidiately return if value if value is not changed by UI click
    async partial void OnExpensesAreFinalisedChanged(bool value)
    {
        if (_blockOnExpensesFinalisedChanged) return;

        // If set to Finalised transfer as much as possible of SavingsGoal to savings
        if (value)
        {
            FinaliseMonth(SelectedDate);
        }

        else
        {
            bool confirm = !IsNextMonthFinalised() || await ConfirmUnfinalise();

            if (confirm)
            {
                UnFinaliseMonth(SelectedDate, false);
            }
            else
            {
                _blockOnExpensesFinalisedChanged = true;
                ExpensesAreFinalised = true;
                _blockOnExpensesFinalisedChanged = false;
                return;
            }
        }

        UpdateFinalizedMonthsDictionary(SelectedDate, value);
    }

    private async Task<bool> ConfirmUnfinalise()
    {
        return await Application.Current.MainPage.DisplayAlert(
            "Are you sure you want to unfinalise expenses?",
            "This will unfinalise all subsequent months' expenses as well and you will have to manually finalise them again.",
            "Go ahead",
            "Undo"
        );
    }

    private void UpdateFinalizedMonthsDictionary(DateTime selectedDate, bool value)
    {
        var currencyMonthKey = GenerateFinalisedMonthsDictKey(selectedDate);
        _finalisedMonthsDictionary[currencyMonthKey] = value;
    }

    private bool IsNextMonthFinalised()
    {
        var startOfCurrentMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
        var nextMonth = SelectedDate.AddMonths(1);
        var key = GenerateFinalisedMonthsDictKey(nextMonth);

        if (_finalisedMonthsDictionary.TryGetValue(key, out bool final) && final)
            return true;
        else
            return false;
    }
    private string GenerateFinalisedMonthsDictKey(DateTime date)
    {
        return $"{SelectedCurrency}:{date.ToString("Y")}";
    }
    private void AddFinalisationTransactions(DateTime month)
    {
        var spendingBudet = ProjectsQuery.CalculateMonthProfitForCurrency(SelectedDate, SelectedCurrency) + GetNetNonSavingsConversionsAmount(month);

        if (TotalSpendingBudget < 0)
        {
            AddOverallLossAsExpenseTransaction(spendingBudet, month);
        }

        var savingsGoal = SelectedSavingsCategoryViewModel.CalculateCumulSavingsGoal(month);
        var totalCumulRemainingBudget = month == SelectedDate ? TotalCumulRemainingBudget : CalculateTotalCumulRemainingBudget(month);

        // Savings Goal is partly or fully reached
        if ((totalCumulRemainingBudget >= 0 || Math.Abs(totalCumulRemainingBudget) < savingsGoal) && savingsGoal != 0)
        {
            var amountToTransfer = Math.Min(savingsGoal, savingsGoal + totalCumulRemainingBudget);
            AddReachedSavingsGoalPortionToSavings(amountToTransfer, month);
        }

        // Expenses exceed budget
        else if (Math.Abs(totalCumulRemainingBudget) > savingsGoal)
        {
            var amountToCoverFromSavings = Math.Abs(totalCumulRemainingBudget + savingsGoal);
            AddSpendingOverdraftToSavingsExpenses(amountToCoverFromSavings, month);
        }

        void AddOverallLossAsExpenseTransaction(decimal loss, DateTime month)
        {
            var monthLossExpense = new ExpenseTransaction(loss, month, Transaction.LossExpenseDescription);
            SelectedSavingsCategoryViewModel.AddIncomingSavingsExpense(monthLossExpense);
        }
        void AddReachedSavingsGoalPortionToSavings(decimal amount, DateTime month)
        {
            var savingsGoalTransfer = new TransferTransaction(Transaction.SavingsGoalTransferSource, amount, month, "Savings");
            SelectedSavingsCategoryViewModel.AddIncomingSavingsTransfer(savingsGoalTransfer);
        }
        void AddSpendingOverdraftToSavingsExpenses(decimal amount, DateTime month)
        {
            var newExpense = new ExpenseTransaction(amount, month, Transaction.SpendingsOverdraftExpenseDescription);
            SelectedSavingsCategoryViewModel.AddIncomingSavingsExpense(newExpense);
        }
    }
    private void RemoveFinalisationTransactions(DateTime month)
    {
        var manualExtraSavingsTransfers = SelectedCurrencySpendingCategoryViewModels.SelectMany(x => x.GetMonthTransactions(month))
                                                                                    .OfType<TransferTransaction>();

        var finalisationTransactions = SelectedSavingsCategoryViewModel.GetMonthTransactions(month).Where(t => t.IsFinalisationTransaction);
        var transactionsToRemove = manualExtraSavingsTransfers.Union(finalisationTransactions).ToList();
        RemoveTransactions(transactionsToRemove);
    }
    private void FinaliseMonth(DateTime month)
    {
        AddFinalisationTransactions(month);
        UpdateFinalizedMonthsDictionary(SelectedDate, true);

    }
    private void UnFinaliseMonth(DateTime month, bool recursiveCall)
    {
        RemoveFinalisationTransactions(month);
        UpdateFinalizedMonthsDictionary(month, false);

        if (!recursiveCall)
        {
            UnFinaliseSubsequentMonths(month.AddMonths(1));
        }

        void UnFinaliseSubsequentMonths(DateTime month)
        {
            var startOfCurrentMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);

            for (var dateToCheck = month; dateToCheck <= startOfCurrentMonth; dateToCheck = dateToCheck.AddMonths(1))
            {
                var key = GenerateFinalisedMonthsDictKey(dateToCheck);

                if (_finalisedMonthsDictionary.TryGetValue(key, out bool final) && final)
                {
                    UnFinaliseMonth(dateToCheck, true);
                }
            }
        }
    }
    private DateTime GetEarliestUnfinalisedMonth()
    {
        var today = DateTime.Today;
        var startCurrentMonth = new DateTime(today.Year, today.Month, 1);
        for (var date = MinDate; date < startCurrentMonth; date = date.AddMonths(1))
        {
            var key = GenerateFinalisedMonthsDictKey(date);
            if (_finalisedMonthsDictionary.TryGetValue(key, out bool result) && !result)
            {
                return date;
            }
        }

        return startCurrentMonth;
    }
    private decimal CalculateTotalCumulRemainingBudget(DateTime month)
    {
        var result = 0m;

        foreach (var cat in SelectedCurrencySpendingCategoryViewModels)
        {
            result += cat.GetCumulativeRemainingBudget(month);
        }

        return result;
    }

    private void SetExpensesAreFinalisedFromDictionary()
    {
        // Set to true so OnExpensesAreFinalisedChanged immidiately returns
        _blockOnExpensesFinalisedChanged = true;
        var currencyMonthKey = GenerateFinalisedMonthsDictKey(SelectedDate);

        if (_finalisedMonthsDictionary.TryGetValue(currencyMonthKey, out bool result))
        {
            ExpensesAreFinalised = result;
        }

        else
        {
            ExpensesAreFinalised = false;
        }
        _blockOnExpensesFinalisedChanged = false;
    }
    [RelayCommand]
    public async Task AddNewTransactionToAllSpendingCategories()
    {
        //Add expense if expenses aren't finalised yet
        if (!ExpensesAreFinalised)
        {
            foreach (var cat in SelectedCurrencySpendingCategoryViewModels)
            {
                cat.AddNewExpense("");
            }
        }

        //Transfer from remaining balance to savings if expenses are finalised
        else
        {
            bool validTransfers = SelectedCurrencySpendingCategoryViewModels.Any(x => x.NewTransactionAmount > 0 && x.NewTransactionAmount <= x.CumulativeRemainingBudget);

            bool confirm = !validTransfers || !IsNextMonthFinalised() || await ConfirmSavingsTransfers();
            if (confirm)
            {
                foreach (var cat in SelectedCurrencySpendingCategoryViewModels)
                {
                    cat.AddNewsTransfer(SelectedSavingsCategoryViewModel);
                }
            }
        }

        OnPropertyChanged(nameof(TotalCumulRemainingBudget));
        OnPropertyChanged(nameof(IsSavingsGoalReached));
    }

    private async Task<bool> ConfirmSavingsTransfers()
    {
        return await Application.Current.MainPage.DisplayAlert(
            "Are you sure you want to transfer extra budget to savings?",
            "This will impact future cumulative budgets and therefore unfinalise all subsequent months.\nIf you go ahead you will have to manually finalise them again.",
            "Go ahead",
            "Cancel"
        );
    }


    [RelayCommand]
    public void AddNewSavingsTransaction()
    {
        SelectedSavingsCategoryViewModel.AddNewTransaction();
    }

    [RelayCommand]
    // Removes transaction from all associated categories
    public void RemoveTransaction(Transaction transaction)
    {
        if (transaction.IsFinalisationTransaction && ExpensesAreFinalised)
        {
            Application.Current.MainPage.DisplayAlert("Unable to delete transaction!", "This transaction is the automatically calculated result based on the Savings Goal and the Total Cumulative Remaining Budget.\n\nThis can only be automatically removed by setting the month as Unfinalised.", "Ok.");
            return;
        }

        if (transaction is ExpenseTransaction && ExpensesAreFinalised)
        {
            Application.Current.MainPage.DisplayAlert("Unable to delete transaction!", "Expenses for this month have been finalised and cannot be deleted.\nSet the current month to unfinalised if you want to edit expenses.", "Ok.");
            return;
        }

        if (transaction is CurrencyConversion conv)
        {
            CurrencyConversions.Remove(conv);
            SelectedCurrencyConversions.Remove(conv);

            // If spending budget affected
            if ((conv.FromCurrency == SelectedCurrency && !conv.IsFromSavings) || (conv.ToCurrency == SelectedCurrency && !conv.IsToSavings))
            {
                OnPropertyChanged(nameof(TotalSpendingBudget));
                SetCategoriesBudget();
            }

            if (conv.IsFromSavings || conv.IsToSavings)
            {
                var affectedSavings = SavingsCategoryViewModels.Where(x => (x.Currency == conv.ToCurrency && conv.IsToSavings) || (x.Currency == conv.FromCurrency && conv.IsFromSavings));
                foreach (var s in affectedSavings)
                {
                    s.RemoveTransaction(conv);
                }
            }
            return;
        }

        else
        {
            var categoryToRemoveFrom = SelectedCurrencySpendingCategoryViewModels.Where(cat => cat.Expenses.Contains(transaction) || cat.Transfers.Contains(transaction)).FirstOrDefault();
            categoryToRemoveFrom?.RemoveTransaction(transaction);
            SelectedSavingsCategoryViewModel.RemoveTransaction(transaction);

            OnPropertyChanged(nameof(TotalCumulRemainingBudget));
            OnPropertyChanged(nameof(IsSavingsGoalReached));
        }
    }

    private void RemoveTransactions(IEnumerable<Transaction> transactions)
    {
        foreach (var t in transactions)
        {
            RemoveTransaction(t);
        }
    }

    [RelayCommand]
    public void AddNewCategory()
    {
        SpendingCategory selectedCurrencyCategory = new(string.Empty, SelectedCurrency);
        SpendingCategoryViewModel vm = new(selectedCurrencyCategory, SelectedDate);
        vm.SetBudgetAndDate(TotalSpendingBudget, SelectedDate);
        SpendingCategoryViewModels.Add(vm);
        SelectedCurrencySpendingCategoryViewModels.Add(vm);
        EditMode = true;
    }

    [RelayCommand]
    public async Task RemoveCategory(SpendingCategoryViewModel cat)
    {
        bool confirmed = await ConfirmCategoryDeletion(cat);

        if (confirmed)
        {
            RemoveCategoryAllTransactions(cat);

            SpendingCategoryViewModels.Remove(cat);
            SelectedCurrencySpendingCategoryViewModels.Remove(cat);

            ResetSavingsCategoryIfNeeded();
        }
        var startOfCurrentMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);

        for (DateTime dateToCheck = SelectedSavingsCategoryViewModel.PercentageHistory.Keys.Min(); dateToCheck <= startOfCurrentMonth; dateToCheck = dateToCheck.AddMonths(1))
        {
            if (!PercentageSumEquals100(dateToCheck))
            {
                UnFinaliseMonth(dateToCheck, false);
                return;
            }
        }

    }

    private async Task<bool> ConfirmCategoryDeletion(SpendingCategoryViewModel cat)
    {
        if (cat.AllTransactions.Count != 0)
        {
            return await DisplayCategoryDeletionConfirmationDialog("Are you sure you want to delete this category?", "You will lose its entire transaction history!\n\nIf you no longer wish to allocate budget to this category, consider setting its Percentage to 0 instead of deleting the entire category.");
        }

        return true;
    }

    private void RemoveCategoryAllTransactions(SpendingCategoryViewModel cat)
    {
        // Makes sure all transactions impacting Savings are removed from savings as well
        foreach (var transaction in cat.AllTransactions.ToList())
        {
            RemoveTransaction(transaction);
        }
    }

    private void ResetSavingsCategoryIfNeeded()
    {
        if (SelectedCurrencySpendingCategoryViewModels.Count == 0)
        {
            SelectedSavingsCategoryViewModel.Percentage = 100;
            EditMode = false;
        }
    }

    private async Task<bool> DisplayCategoryDeletionConfirmationDialog(string title, string message)
    {
        return await Application.Current.MainPage.DisplayAlert(title, message, "Delete Anyway", "Cancel");
    }

    [RelayCommand(CanExecute = nameof(CanAddCurrencyConversion))]
    public void AddCurrencyConversion()
    {
        decimal budgetLimit = IsFromSavingsConversion ? SelectedSavingsCategoryViewModel.TotalSavingsUpToSelectedDate : TotalSpendingBudget;
        string balance = IsFromSavingsConversion ? "Savings" : "Total Budget";

        if (NewFromAmountEntry > budgetLimit)
        {
            Application.Current.MainPage.DisplayAlert("Insufficient Balance", $"You cannot convert more than the {balance} balance!", "Ok");
            return;
        }

        var newConversion = new CurrencyConversion(SelectedCurrency, IsFromSavingsConversion, NewToCurrencyEntry, IsToSavingsConversion, NewFromAmountEntry, NewToAmountEntry, SelectedDate);

        if (CurrencyConversions.Any(x => x.Date > SelectedDate))
        {
            var index = CurrencyConversions.Where(x => x.Date < SelectedDate).Count();
            CurrencyConversions.Insert(index, newConversion);
        }
        else
        {
            CurrencyConversions.Add(newConversion);
        }

        // Add conversions to relevant SavingsViewModels if conversions are to/from savings
        if (newConversion.IsFromSavings)
        {
            var fromSavingsVM = SavingsCategoryViewModels.FirstOrDefault(x => x.Currency == newConversion.FromCurrency);
            fromSavingsVM.AddIncomingSavingsConversion(newConversion);
        }

        if (newConversion.IsToSavings)
        {
            var toSavingsVM = SavingsCategoryViewModels.FirstOrDefault(x => x.Currency == newConversion.ToCurrency);
            toSavingsVM.AddIncomingSavingsConversion(newConversion);
        }

        PopulateSelectedCurrencyConversions();
        OnPropertyChanged(nameof(TotalSpendingBudget));
        SetCategoriesBudget();

        NewToAmountEntry = 0;
        NewFromAmountEntry = 0;
    }
    private bool CanAddCurrencyConversion()
    {
        // Cannot convert money from spending balance after finalisation
        return (!(ExpensesAreFinalised && !IsFromSavingsConversion) && NewToAmountEntry != 0 && NewFromAmountEntry != 0 && !String.IsNullOrEmpty(NewToCurrencyEntry));
    }

    private void SetCategoriesBudget()
    {
        foreach (var category in SelectedCurrencySpendingCategoryViewModels)
        {
            category.SetBudget(TotalSpendingBudget);
        }

        SelectedSavingsCategoryViewModel.UpdateSavingsGoalUI();
    }
    private void SetCategoriesBudgetandDate()
    {
        foreach (var category in SelectedCurrencySpendingCategoryViewModels)
        {
            category.SetBudgetAndDate(TotalSpendingBudget, SelectedDate);
        }

        SelectedSavingsCategoryViewModel.SetAndApplyDate(TotalSpendingBudget, SelectedDate);
    }
    private void PopulateSelectedCurrencySpendingCategories()
    {
        SelectedCurrencySpendingCategoryViewModels.Clear();

        foreach (var cat in SpendingCategoryViewModels.Where(c => c.Currency == SelectedCurrency))
        {
            SelectedCurrencySpendingCategoryViewModels.Add(cat);
        }
    }
    private void PopulateSelectedCurrencyConversions()
    {
        // Create new to force template selection
        SelectedCurrencyConversions = new(FromSelectedCurrencySavingsConversions.Union(ToSelectedCurrencySavingsConversions)
                                                                                .Union(FromSelectedCurrencyNonSavingsConversions)
                                                                                .Union(ToSelectedCurrencyNonSavingsConversions)
                                                                                .OrderBy(x => x.Date));
    }

    private decimal GetNetNonSavingsConversionsAmount(DateTime date)
    {
        var outgoing = CurrencyConversions.Where(c => c.Date == date && c.FromCurrency == SelectedCurrency && !c.IsFromSavings).Sum(c => c.Amount);
        var incoming = CurrencyConversions.Where(c => c.Date == date && c.ToCurrency == SelectedCurrency && !c.IsToSavings).Sum(c => c.ToAmount);

        return incoming - outgoing;
    }
    private async void CheckCurrentMonthPercentages()
    {
        if (!PercentageSumEquals100(SelectedDate))
        {
            await Application.Current.MainPage.DisplayAlert("Invalid Percentages Total", "Category percentages + Savings Goal must add up to 100%", "Ok");
            if (EditMode == false)
            {
                EditMode = true;
            }
        }
        else
        {
            OnPropertyChanged(nameof(TotalCumulRemainingBudget));
            OnPropertyChanged(nameof(IsSavingsGoalReached));
            await SaveAllSpendingCategoriesAndCurrencyConversions();
        }
    }
    private bool PercentageSumEquals100(DateTime date)
    {
        return SelectedCurrencySpendingCategoryViewModels.Sum(x => x.GetDatePercentage(date)) + SelectedSavingsCategoryViewModel.GetDatePercentage(date) == 100m;
    }
    private async void CheckCategoryNamesAreUnique()
    {
        if (SelectedCurrencySpendingCategoryViewModels.Select(x => x.Name).Distinct().Count() != SelectedCurrencySpendingCategoryViewModels.Count)
        {
            await Application.Current.MainPage.DisplayAlert("Duplicate Category Names!", "All category names must be unique.", "Ok.");
            if (EditMode == false)
            {
                EditMode = true;
            }
        }
    }
    private void CheckForAndCreateMissingSavingsViewModels()
    {
        foreach (var cur in CurrencyList.Where(c => !SavingsCategoryViewModels.Any(cat => cat.Currency == c)))
        {
            SpendingCategory newSavings = new("Savings", cur);
            SavingsCategoryViewModel newSavingsVM = new(newSavings);
            SavingsCategoryViewModels.Add(newSavingsVM);
        }
    }
    private async Task SaveAllSpendingCategoriesAndCurrencyConversions()
    {
        if (PercentageSumEquals100(SelectedDate))
        {
            SaveAllCurrencyConversions();
            await SpendingOverviewDataManager.WriteToJsonAsync(SpendingCategoryViewModels.Select(x => x.Category), SavingsCategoryViewModels.Select(x => x.Category), _finalisedMonthsDictionary);
        }

    }
    private void SaveAllCurrencyConversions()
    {
        CurrencyConversionManager.WriteToJson();
    }

    public enum SavingsGoalReachedStatus
    {
        Fully,
        Partly,
        Zero,
        Negative
    }
}
