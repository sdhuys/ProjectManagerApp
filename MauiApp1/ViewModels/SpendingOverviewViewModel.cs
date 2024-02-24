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
    DateTime selectedDate = new(DateTime.Today.Year, DateTime.Today.Month, 1);

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

    public decimal SelectedCurrencyNetSavingsConversions => ToSelectedCurrencySavingsConversions.Sum(x => x.ToAmount) - FromSelectedCurrencySavingsConversions.Sum(x => x.Amount);
    public decimal SelectedCurrencyNetNonSavingsConversions => ToSelectedCurrencyNonSavingsConversions.Sum(x => x.ToAmount) - FromSelectedCurrencyNonSavingsConversions.Sum(x => x.Amount);

    public decimal ActualProfitForCurrency => ProjectsQuery.CalculateMonthProfitForCurrency(SelectedDate, SelectedCurrency);
    public decimal TotalSpendingBudget => ActualProfitForCurrency + SelectedCurrencyNetNonSavingsConversions;
    public decimal TotalCumulRemainingBudget => SelectedCurrencySpendingCategoryViewModels.Sum(cat => cat.CumulativeRemainingBudget);
    public SavingsGoalReachedStatus IsSavingsGoalReached
    {
        get
        {
            if (TotalCumulRemainingBudget >= 0)
                return SavingsGoalReachedStatus.Fully;
            else if (Math.Abs(TotalCumulRemainingBudget) < SelectedSavingsCategoryViewModel.CumulativeSavingsGoal)
                return SavingsGoalReachedStatus.Partly;
            else if (Math.Abs(TotalCumulRemainingBudget) == SelectedSavingsCategoryViewModel.CumulativeSavingsGoal)
                return SavingsGoalReachedStatus.Zero;
            else
                return SavingsGoalReachedStatus.Negative;
        }
    }


    // Dictionary containing keys in "CURRENCY:MM/YY" format, values representing whether or not that months' accounting is finalized
    private Dictionary<string, bool> _finalizedMonthsDictionary;

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
        _finalizedMonthsDictionary = dict;

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
        CheckPercentages();
        SetExpensesAreFinalisedFromDictionary();
    }

    partial void OnSelectedDateChanged(DateTime oldValue, DateTime newValue)
    {
        SelectedDate = new DateTime(newValue.Year, newValue.Month, 1);

        if (newValue.Month != oldValue.Month || newValue.Year != oldValue.Year)
        {
            SetCategoriesBudgetandDate();
            PopulateSelectedCurrencyConversions();
            OnPropertyChanged(nameof(TotalSpendingBudget));
            CheckPercentages();
            SetExpensesAreFinalisedFromDictionary();
        }
    }

    partial void OnEditModeChanged(bool value)
    {
        if (!value)
        {
            CheckCategoryNamesAreUnique();
            CheckPercentages();
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
    // Immidiately return if value is set from dictionary on page load/date change (= not a real change)
    partial void OnExpensesAreFinalisedChanged(bool oldValue, bool newValue)
    {
        if (_blockOnExpensesFinalisedChanged) return;

        // If set to Finalised transfer as much as possible of SavingsGoal to savings
        if (newValue)
        {
            if (TotalSpendingBudget < 0)
            {
                var monthLossExpense = new ExpenseTransaction(Math.Abs(TotalSpendingBudget), SelectedDate, "Overall Loss Coverage");
                SelectedSavingsCategoryViewModel.AddIncomingSavingsExpense(monthLossExpense);
            }
            decimal savingsGoal = SelectedSavingsCategoryViewModel.CumulativeSavingsGoal;

            // 2 == SavingsGoalReachedStatus.Zero
            if ((int)IsSavingsGoalReached < 2)
            {
                var amountToTransfer = Math.Min(savingsGoal, savingsGoal + TotalCumulRemainingBudget);
                if (amountToTransfer == 0) return;

                var savingsGoalTransfer = new TransferTransaction("Savings Goal Portion", amountToTransfer, SelectedDate, "Savings");
                SelectedSavingsCategoryViewModel.AddIncomingSavingsTransfer(savingsGoalTransfer);
            }

            else if ((int)IsSavingsGoalReached > 2)
            {
                var monthSavingsGoal = SelectedSavingsCategoryViewModel.SavingsGoal;
                var amountToCoverFromSavings = monthSavingsGoal + TotalCumulRemainingBudget;
                var newExpense = new ExpenseTransaction(Math.Abs(amountToCoverFromSavings), SelectedDate, "Spendings Overdraft Coverage");
                SelectedSavingsCategoryViewModel.AddIncomingSavingsExpense(newExpense);
            }
        }

        else
        {
            var manualExtraSavingsTransfers = SelectedCurrencySpendingCategoryViewModels.SelectMany(x => x.SelectedMonthTransactions)
                                                                                        .OfType<TransferTransaction>()
                                                                                        .ToList();
            foreach (var transfer in manualExtraSavingsTransfers)
            {
                var spendingCat = SelectedCurrencySpendingCategoryViewModels.Where(x => x.Name == transfer.Source).FirstOrDefault();
                spendingCat.RemoveTransaction(transfer);
                SelectedSavingsCategoryViewModel.RemoveTransaction(transfer);
            }

            var lossExpense = SelectedSavingsCategoryViewModel.SelectedMonthTransactions.Where(x => x is ExpenseTransaction e && e.Description == "Overall Loss Coverage").FirstOrDefault();
            var overdraftExpense = SelectedSavingsCategoryViewModel.SelectedMonthTransactions.Where(x => x is ExpenseTransaction e && e.Description == "Spendings Overdraft Coverage").FirstOrDefault();
            var goalTransfer = SelectedSavingsCategoryViewModel.SelectedMonthTransactions.Where(x => x is TransferTransaction t && t.Source == "Savings Goal Portion").FirstOrDefault();
            SelectedSavingsCategoryViewModel.RemoveTransaction(lossExpense);
            SelectedSavingsCategoryViewModel.RemoveTransaction(goalTransfer);
            SelectedSavingsCategoryViewModel.RemoveTransaction(overdraftExpense);
        }

        var currencyMonthKey = $"{SelectedCurrency}:{SelectedDate.ToString("Y")}";
        _finalizedMonthsDictionary[currencyMonthKey] = newValue;
    }
    private void SetExpensesAreFinalisedFromDictionary()
    {
        // Set to true so OnExpensesAreFinalisedChanged immidiately returns
        _blockOnExpensesFinalisedChanged = true;
        var currencyMonthKey = $"{SelectedCurrency}:{SelectedDate:Y}";
        if (_finalizedMonthsDictionary.ContainsKey(currencyMonthKey))
        {
            ExpensesAreFinalised = _finalizedMonthsDictionary[currencyMonthKey];
        }

        else
        {
            ExpensesAreFinalised = false;
        }
        _blockOnExpensesFinalisedChanged = false;
    }
    [RelayCommand]
    public void AddNewTransactionToAllSpendingCategories()
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
            foreach (var cat in SelectedCurrencySpendingCategoryViewModels)
            {
                cat.AddNewsTransfer(SelectedSavingsCategoryViewModel);
            }
        }

        OnPropertyChanged(nameof(TotalCumulRemainingBudget));
        OnPropertyChanged(nameof(IsSavingsGoalReached));
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

    [RelayCommand]
    public void AddNewCategory()
    {
        SpendingCategory selectedCurrencyCategory = new(string.Empty, SelectedCurrency);
        SpendingCategoryViewModel vm = new(selectedCurrencyCategory);
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

        SelectedSavingsCategoryViewModel.CalculateSavingsGoal();
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

    private async void CheckPercentages()
    {
        if (!PercentageSumEquals100())
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
    private bool PercentageSumEquals100()
    {
        return SelectedCurrencySpendingCategoryViewModels.Sum(x => x.Percentage) + SelectedSavingsCategoryViewModel.Percentage == 100m;
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
        if (PercentageSumEquals100())
        {
            SaveAllCurrencyConversions();
            await SpendingOverviewDataManager.WriteToJson(SpendingCategoryViewModels.Select(x => x.Category), SavingsCategoryViewModels.Select(x => x.Category), _finalizedMonthsDictionary);
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
