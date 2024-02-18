using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MauiApp1.Models;
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
                                                                                      CurrencyConversions.Where(c => c.FromCurrency == SelectedCurrency && c.IsToSavings
                                                                                      && c.Date.Month == SelectedDate.Month && c.Date.Year == SelectedDate.Year)
                                                                                      : CurrencyConversions.Where(c => c.FromCurrency == SelectedCurrency && c.IsToSavings);
    private IEnumerable<CurrencyConversion> FromSelectedCurrencyNonSavingsConversions => !ShowAllTransactions ?
                                                                                         CurrencyConversions.Where(c => c.FromCurrency == SelectedCurrency && !c.IsToSavings
                                                                                         && c.Date.Month == SelectedDate.Month && c.Date.Year == SelectedDate.Year)
                                                                                         : CurrencyConversions.Where(c => c.FromCurrency == SelectedCurrency && !c.IsToSavings);

    [ObservableProperty]
    ObservableCollection<CurrencyConversion> selectedCurrencyConversions;

    public decimal SelectedCurrencyNetSavingsConversions => ToSelectedCurrencySavingsConversions.Sum(x => x.ToAmount) - FromSelectedCurrencySavingsConversions.Sum(x => x.Amount);
    public decimal SelectedCurrencyNetNonSavingsConversions => ToSelectedCurrencyNonSavingsConversions.Sum(x => x.ToAmount) - FromSelectedCurrencyNonSavingsConversions.Sum(x => x.Amount);

    public decimal ActualProfitForCurrency => ShowAllTransactions ? GetSelectedMonthNetProfit()
                                                                : ProjectManager.AllProjects.Where(p => p.Currency == SelectedCurrency)
                                                                                            .Select(p => new ProjectViewModel(p))
                                                                                            .Sum(p => p.ActualProfit);
    public decimal TotalSpendingBudget => ActualProfitForCurrency + SelectedCurrencyNetNonSavingsConversions;

    // Includes the savings goal portion
    public decimal TotalRemainingBudget => TotalSpendingBudget - SelectedCurrencySpendingCategoryViewModels.Sum(cat => cat.SelectedMonthTransactions.Sum(x => x.Amount));
    public bool IsSavingsGoalReached => TotalRemainingBudget >= SelectedSavingsCategoryViewModel?.SavingsGoal;

    // Dictionary containing keys in "CURRENCY//MM/YY" format, values representing whether or not that months' accounting is finalized
    private Dictionary<string, bool> _finalizedMonthsDictionary;

    [ObservableProperty]
    bool expensesAreFinalised;

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

        CurrencyConversions = new(CurrencyConversionManager.LoadFromJson().OrderBy(x => x.Date));
        SelectedCurrencyConversions = new();
        SelectedCurrencySpendingCategoryViewModels = new();
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
        SetCategoriesTotalBudgets();
        SetAndApplyCategoriesDates();
        OnPropertyChanged(nameof(NonSelectedCurrencies));
        CheckPercentages();
    }

    partial void OnSelectedDateChanged(DateTime oldValue, DateTime newValue)
    {
        SelectedDate = new DateTime(newValue.Year, newValue.Month, 1);

        if (newValue.Month != oldValue.Month || newValue.Year != oldValue.Year)
        {
            SetCategoriesTotalBudgets();
            SetAndApplyCategoriesDates();
            PopulateSelectedCurrencyConversions();
            OnPropertyChanged(nameof(TotalSpendingBudget));
            CheckPercentages();
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
                cat.AddNewsTransfer(SelectedDate, SelectedSavingsCategoryViewModel.Category);
            }
        }

        //CalculateSavingsPotential();
        OnPropertyChanged(nameof(TotalRemainingBudget));
        OnPropertyChanged(nameof(IsSavingsGoalReached));
    }

    [RelayCommand]
    public void RemoveTransaction(Transaction transaction)
    {
        var categoryToRemoveFrom = SelectedCurrencySpendingCategoryViewModels.Where(cat => cat.Expenses.Contains(transaction) || cat.Transfers.Contains(transaction)).FirstOrDefault();
        categoryToRemoveFrom.RemoveTransaction(transaction);

        //CalculateSavingsPotential();
        OnPropertyChanged(nameof(TotalRemainingBudget));
        OnPropertyChanged(nameof(IsSavingsGoalReached));
    }

    [RelayCommand]
    public void AddNewCategory()
    {
        SpendingCategory selectedCurrencyCategory = new(string.Empty, SelectedCurrency);
        SpendingCategoryViewModel vm = new(selectedCurrencyCategory);
        vm.SetBudget(TotalSpendingBudget);
        vm.SetAndApplyDate(SelectedDate);
        SpendingCategoryViewModels.Add(vm);
        SelectedCurrencySpendingCategoryViewModels.Add(vm);
        EditMode = true;
    }

    [RelayCommand]
    public void RemoveCategory(SpendingCategoryViewModel cat)
    {
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

        PopulateSelectedCurrencyConversions();
        OnPropertyChanged(nameof(TotalSpendingBudget));
        SetCategoriesTotalBudgets();
        SaveAllCurrencyConversions();

        NewToAmountEntry = 0;
        NewFromAmountEntry = 0;
    }
    private bool CanAddCurrencyConversion()
    {
        return (NewToAmountEntry != 0 && NewFromAmountEntry != 0 && !String.IsNullOrEmpty(NewToCurrencyEntry));
    }

    [RelayCommand]
    public void RemoveCurrencyConversion(CurrencyConversion conv)
    {
        CurrencyConversions.Remove(conv);
        SelectedCurrencyConversions.Remove(conv);

        OnPropertyChanged(nameof(TotalSpendingBudget));
        SetCategoriesTotalBudgets();
    }
    private void SetCategoriesTotalBudgets()
    {
        foreach (var category in SelectedCurrencySpendingCategoryViewModels)
        {
            category.SetBudget(TotalSpendingBudget);
        }

        SelectedSavingsCategoryViewModel.SetBudget(TotalSpendingBudget);
    }
    private void SetAndApplyCategoriesDates()
    {
        foreach (var category in SelectedCurrencySpendingCategoryViewModels)
        {
            category.SetAndApplyDate(SelectedDate);
        }

        SelectedSavingsCategoryViewModel.SetAndApplyDate(SelectedDate);
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
        CurrencyConversionManager.WriteToJson(CurrencyConversions);
    }
    private decimal GetSelectedMonthNetProfit()
    {
        var payments = PaymentQueryManager.QueryByCurrencyAndMonth(SelectedCurrency, SelectedDate).Select(x => new PaymentViewModel(x));
        var totalPaymentsAmount = payments.Sum(p => p.Amount);
        var totalPaymentsVatAmount = payments.Sum(p => p.VatAmount);
        var totalRelatedExpenses = PaymentsRelatedExpensesCalculator.CalculateRelatedExpenses(payments, SelectedDate);

        return totalPaymentsAmount - (totalRelatedExpenses + totalPaymentsVatAmount);
    }
}
