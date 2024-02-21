using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MauiApp1.Models;
using MauiApp1.StaticHelpers;
using System.Collections.ObjectModel;

namespace MauiApp1.ViewModels;

public partial class SpendingOverviewViewModel : ObservableObject
{
    [ObservableProperty]
    bool showAllTransactions;

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

    public decimal ActualProfitForCurrency => MonthlyProfitCalculator.CalculateMonthProfitForCurrency(SelectedDate, SelectedCurrency);
    public decimal TotalSpendingBudget => ActualProfitForCurrency + SelectedCurrencyNetNonSavingsConversions;
    public decimal TotalRemainingBudget => SelectedCurrencySpendingCategoryViewModels.Sum(cat => cat.CumulativeRemainingBudget);
    public SavingsGoalReachedStatus IsSavingsGoalReached
    {
        get
        {
            if (TotalRemainingBudget >= 0)
                return SavingsGoalReachedStatus.Fully;
            else if (Math.Abs(TotalRemainingBudget) < SelectedSavingsCategoryViewModel.SavingsGoal)
                return SavingsGoalReachedStatus.Partly;
            else if (Math.Abs(TotalRemainingBudget) == SelectedSavingsCategoryViewModel.SavingsGoal)
                return SavingsGoalReachedStatus.Zero;
            else
                return SavingsGoalReachedStatus.Negative;
        }
    }


    // Dictionary containing keys in "CURRENCY:MM/YY" format, values representing whether or not that months' accounting is finalized
    private Dictionary<string, bool> _finalizedMonthsDictionary;

    [ObservableProperty]
    bool expensesAreFinalised;

    private bool _blockOnExpensesFinalisedChanged;

    [ObservableProperty]
    bool editMode;

    [ObservableProperty]
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
        var jsonData = SpendingOverviewDataManager.LoadFromJson();
        SpendingCategoryViewModels = jsonData.spendings.Select(x => new SpendingCategoryViewModel(x)).ToList();
        SavingsCategoryViewModels = jsonData.savings.Select(x => new SavingsCategoryViewModel(x)).ToList();
        _finalizedMonthsDictionary = jsonData.dict;

        CurrencyConversions = CurrencyConversionManager.LoadFromJson();
        SelectedCurrencyConversions = new();
        SelectedCurrencySpendingCategoryViewModels = new();

        _currencyList = ProjectManager.AllProjects.Select(p => p.Currency).Distinct().ToList();
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
                fromSavingsVM?.AddNewSavingsConversion(conv);
            }
            if (conv.IsToSavings)
            {
                var toSavingsVM = SavingsCategoryViewModels.FirstOrDefault(x => x.Currency == conv.ToCurrency);
                toSavingsVM?.AddNewSavingsConversion(conv);
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

        // If new currency gets displayed with this bool = true, switching values will not correctly display selected-month/all transactions
        ShowAllTransactions = false;

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
        ShowAllTransactions = false;

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
            CheckPercentages();
        }
    }

    partial void OnShowAllTransactionsChanged(bool value)
    {
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
            decimal savingsGoal = SelectedSavingsCategoryViewModel.SavingsGoal;

            // 2 == SavingsGoalReachedStatus.Zero
            if ((int)IsSavingsGoalReached < 2)
            {
                var amountToTransfer = Math.Min(savingsGoal, savingsGoal + TotalRemainingBudget);

                var savingsGoalTransfer = new TransferTransaction("SavingsGoalPortion", amountToTransfer, SelectedDate, "Savings");
                SelectedSavingsCategoryViewModel.AddIncomingSavingsTransfer(savingsGoalTransfer);
            }

            else if ((int)IsSavingsGoalReached > 2)
            {
                var amountToCoverFromSavings = savingsGoal + TotalRemainingBudget;
                var newExpense = new ExpenseTransaction(Math.Abs(amountToCoverFromSavings), SelectedDate, "Spendings Overdraft Coverage");
                SelectedSavingsCategoryViewModel.AddIncomingSavingsExpense(newExpense);
            }
        }

        else
        {
            var expense = SelectedSavingsCategoryViewModel.SelectedMonthTransactions.Where(x => x is ExpenseTransaction e && e.Description == "Spendings Overdraft Coverage").FirstOrDefault();
            var transfer = SelectedSavingsCategoryViewModel.SelectedMonthTransactions.Where(x => x is TransferTransaction t && t.Source == "SavingsGoalPortion").FirstOrDefault();
            SelectedSavingsCategoryViewModel.RemoveTransaction(transfer);
            SelectedSavingsCategoryViewModel.RemoveTransaction(expense);
        }

        var currencyMonthKey = $"{SelectedCurrency}:{SelectedDate.ToString("Y")}";
        _finalizedMonthsDictionary[currencyMonthKey] = newValue;
    }
    private void SetExpensesAreFinalisedFromDictionary()
    {
        // Set to true so OnExpensesAreFinalisedChanged immidiately returns
        _blockOnExpensesFinalisedChanged = true;
        var currencyMonthKey = $"{SelectedCurrency}:{SelectedDate.ToString("Y")}";
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
                cat.AddNewExpense(SelectedDate, "");
            }
        }

        //Transfer from remaining balance to savings if expenses are finalised
        else
        {
            foreach (var cat in SelectedCurrencySpendingCategoryViewModels)
            {
                cat.AddNewsTransfer(SelectedDate, SelectedSavingsCategoryViewModel);
            }
        }

        OnPropertyChanged(nameof(TotalRemainingBudget));
        OnPropertyChanged(nameof(IsSavingsGoalReached));
    }
    public void AddExternalSavingsTransfer()
    {

    }
    [RelayCommand]
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
            categoryToRemoveFrom.RemoveTransaction(transaction);

            if (transaction is TransferTransaction t && t.Destination == SelectedSavingsCategoryViewModel.Name)
            {
                SelectedSavingsCategoryViewModel.RemoveTransaction(t);
            }

            OnPropertyChanged(nameof(TotalRemainingBudget));
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
    public void RemoveCategory(SpendingCategoryViewModel cat)
    {
        // Makes sure all transactions impacting Savings are removed from savings as well
        foreach (var transaction in cat.AllTransactions.ToList())
        {
            RemoveTransaction(transaction);
        }

        SpendingCategoryViewModels.Remove(cat);
        SelectedCurrencySpendingCategoryViewModels.Remove(cat);

        if (SelectedCurrencySpendingCategoryViewModels.Count == 0)
        {
            SelectedSavingsCategoryViewModel.Percentage = 100;
            EditMode = false;
        }
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
            var savingsVM = SavingsCategoryViewModels.FirstOrDefault(x => x.Currency == newConversion.FromCurrency);
            savingsVM.AddNewSavingsConversion(newConversion);
        }

        if (newConversion.IsToSavings)
        {
            var savingsVM = SavingsCategoryViewModels.FirstOrDefault(x => x.Currency == newConversion.ToCurrency);
            savingsVM.AddNewSavingsConversion(newConversion);
        }

        PopulateSelectedCurrencyConversions();
        OnPropertyChanged(nameof(TotalSpendingBudget));
        SetCategoriesBudget();
        SaveAllCurrencyConversions();

        NewToAmountEntry = 0;
        NewFromAmountEntry = 0;
    }
    private bool CanAddCurrencyConversion()
    {
        return (NewToAmountEntry != 0 && NewFromAmountEntry != 0 && !String.IsNullOrEmpty(NewToCurrencyEntry));
    }

    private void SetCategoriesBudget()
    {
        foreach (var category in SelectedCurrencySpendingCategoryViewModels)
        {
            category.SetBudget(TotalSpendingBudget);
        }

        SelectedSavingsCategoryViewModel.SetBudget(TotalSpendingBudget);
    }
    private void SetCategoriesBudgetandDate()
    {
        foreach (var category in SelectedCurrencySpendingCategoryViewModels)
        {
            category.SetBudgetAndDate(TotalSpendingBudget, SelectedDate);
        }

        SelectedSavingsCategoryViewModel.SetBudgetAndDate(TotalSpendingBudget, SelectedDate);
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
            OnPropertyChanged(nameof(TotalRemainingBudget));
            OnPropertyChanged(nameof(IsSavingsGoalReached));
            await SaveAllSpendingCategories();
        }
    }
    private bool PercentageSumEquals100()
    {
        return SelectedCurrencySpendingCategoryViewModels.Sum(x => x.Percentage) + SelectedSavingsCategoryViewModel.Percentage == 100m;
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
    private async Task SaveAllSpendingCategories()
    {
        if (PercentageSumEquals100())
            await SpendingOverviewDataManager.WriteToJson(SpendingCategoryViewModels.Select(x => x.Category), SavingsCategoryViewModels.Select(x => x.Category), _finalizedMonthsDictionary);
    }
    private void SaveAllCurrencyConversions()
    {
        CurrencyConversionManager.WriteToJson();
    }
    private decimal GetSelectedMonthNetProfit()
    {
        var payments = PaymentQueryManager.QueryByCurrencyAndMonth(SelectedCurrency, SelectedDate).Select(x => new PaymentViewModel(x));
        var totalPaymentsAmount = payments.Sum(p => p.Amount);
        var totalPaymentsVatAmount = payments.Sum(p => p.VatAmount);
        var totalRelatedExpenses = PaymentsRelatedExpensesCalculator.CalculateRelatedExpenses(payments, SelectedDate);

        return totalPaymentsAmount - (totalRelatedExpenses + totalPaymentsVatAmount);
    }

    public enum SavingsGoalReachedStatus
    {
        Fully,
        Partly,
        Zero,
        Negative
    }
}
