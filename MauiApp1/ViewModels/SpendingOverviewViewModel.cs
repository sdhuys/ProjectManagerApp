using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MauiApp1.Models;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace MauiApp1.ViewModels;

public partial class SpendingOverviewViewModel : ObservableObject
{
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
    public IEnumerable<CurrencyConversion> ToSelectedCurrencySavingsConversions => CurrencyConversions.Where(c => c.ToCurrency == SelectedCurrency && c.ToSavings);
    public IEnumerable<CurrencyConversion> ToSelectedCurrencyNonSavingsConversions => CurrencyConversions.Where(c => c.ToCurrency == SelectedCurrency && !c.ToSavings);
    public IEnumerable<CurrencyConversion> FromSelectedCurrencySavingsConversions => CurrencyConversions.Where(c => c.FromCurrency == SelectedCurrency && c.ToSavings);
    public IEnumerable<CurrencyConversion> FromSelectedCurrencyNonSavingsConversions => CurrencyConversions.Where(c => c.FromCurrency == SelectedCurrency && !c.ToSavings);

    [ObservableProperty]
    ObservableCollection<CurrencyConversion> selectedSavingsCurrencyConversions;

    [ObservableProperty]
    ObservableCollection<CurrencyConversion> selectedNonSavingsCurrencyConversion;

    public decimal SelectedCurrencyNetSavingsConversions => ToSelectedCurrencySavingsConversions.Sum(x => x.ToAmount) - FromSelectedCurrencySavingsConversions.Sum(x => x.FromAmount);
    public decimal SelectedCurrencyNetNonSavingsConversions => ToSelectedCurrencyNonSavingsConversions.Sum(x => x.ToAmount) - FromSelectedCurrencyNonSavingsConversions.Sum(x => x.FromAmount);

    public decimal ActualProfitForCurrency => ProjectManager.AllProjects.Where(p => p.Currency == SelectedCurrency)
                                                    .Select(p => new ProjectViewModel(p))
                                                    .Sum(p => p.ActualProfit);
    public decimal TotalSpendingBudget => ActualProfitForCurrency + SelectedCurrencyNetNonSavingsConversions;


    [ObservableProperty]
    bool isToSavingsTransaction;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EditModeToOpacity))]
    bool editMode;
    public double EditModeToOpacity => EditMode ? 0 : 1;

    [ObservableProperty]
    DateTime newExpenseDate = DateTime.Today;

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

    [ObservableProperty]
    DateTime newConversionDate = DateTime.Today;

    public SpendingOverviewViewModel()
    {
        var categoriesCollections = SpendingCategoryManager.LoadFromJson();
        SpendingCategoryViewModels = categoriesCollections.spendings.Select(x => new SpendingCategoryViewModel(x)).ToList();
        SavingsCategoryViewModels = categoriesCollections.savings.Select(x => new SavingsCategoryViewModel(x)).ToList();

        CurrencyConversions = new(CurrencyConversionManager.LoadFromJson().OrderBy(x => x.Date));
        SelectedSavingsCurrencyConversions = new();
        SelectedNonSavingsCurrencyConversion = new();
        SelectedCurrencySpendingCategoryViewModels = new();
    }

    public void OnAppearing()
    {
        _currencyList = ProjectManager.AllProjects.Select(p => p.Currency).Distinct().ToList();
        OnPropertyChanged(nameof(CurrencyList));

        CheckForAndCreateMissingSavingsViewModels();

        // Set selected currency to one with highest payments received amount
        SelectedCurrency = CurrencyList.OrderByDescending(c => ProjectManager.AllProjects.Where(p => p.Currency == c).Sum(p => p.Payments.Sum(x => x.Amount))).FirstOrDefault();
    }

    partial void OnSelectedCurrencyChanged(string value)
    {
        if (value == null)
        {
            return;
        }
        PopulateSelectedCurrencyConversions();
        PopulateSelectedCurrencySpendingCategories();
        OnPropertyChanged(nameof(SelectedSavingsCategoryViewModel));
        OnPropertyChanged(nameof(TotalSpendingBudget));
        SetCategoriesTotalBudgets();
        OnPropertyChanged(nameof(NonSelectedCurrencies));
        CheckPercentages();
    }
    partial void OnEditModeChanged(bool value)
    {
        if (!value)
        {
            CheckPercentages();
        }
    }

    [RelayCommand]
    public void AddNewTransactionToAllSpendingCategories()
    {
        //Add logic to determine adding Transfer or Expense
        if (!IsToSavingsTransaction)
        {
            foreach (var cat in SelectedCurrencySpendingCategoryViewModels)
            {
                SpendingCategory.ExpenseTransaction newExpense = new(cat.NewTransactionValue, NewExpenseDate, "");
                cat.AddNewExpense(newExpense);
            }
        }
    }

    [RelayCommand]
    public void RemoveTransaction(SpendingCategory.Transaction expense)
    {
        var categoryToRemoveFrom = SelectedCurrencySpendingCategoryViewModels.Where(cat => cat.Expenses.Contains(expense)).FirstOrDefault();
        categoryToRemoveFrom.RemoveTransaction(expense);

        CalculateSavingPotential();
    }

    [RelayCommand]
    public void AddNewCategory()
    {
        SpendingCategory selectedCurrencyCategory = new(string.Empty, SelectedCurrency);
        SpendingCategoryViewModel vm = new(selectedCurrencyCategory);
        vm.SetBudget(TotalSpendingBudget);
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
        var newConversion = new CurrencyConversion(SelectedCurrency, IsFromSavingsConversion, NewToCurrencyEntry, IsToSavingsConversion, NewFromAmountEntry, NewToAmountEntry, NewConversionDate);

        if (CurrencyConversions.Any(x => x.Date > NewConversionDate))
        {
            var index = CurrencyConversions.Where(x => x.Date < NewConversionDate).Count();
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
        NewConversionDate = DateTime.Today;
    }
    private bool CanAddCurrencyConversion()
    {
        return (NewToAmountEntry != 0 && NewFromAmountEntry != 0 && !String.IsNullOrEmpty(NewToCurrencyEntry));
    }

    [RelayCommand]
    public void RemoveCurrencyConversion(CurrencyConversion conv)
    {
        CurrencyConversions.Remove(conv);

        if (!SelectedSavingsCurrencyConversions.Remove(conv))
        {
            SelectedNonSavingsCurrencyConversion.Remove(conv);
        }
        
        OnPropertyChanged(nameof(TotalSpendingBudget));
        SetCategoriesTotalBudgets();
    }
    private void SetCategoriesTotalBudgets()
    {
        foreach (var category in SelectedCurrencySpendingCategoryViewModels)
        {
            category.SetBudget(TotalSpendingBudget);
        }

        SelectedSavingsCategoryViewModel?.SetBudget(TotalSpendingBudget);
    }
    private void CalculateSavingPotential()
    {
        SelectedSavingsCategoryViewModel.SetOtherCategoreisExpensesTotals(SelectedCurrencySpendingCategoryViewModels.Sum(cat => cat.AllTransactions.Sum(x => x.Amount)));
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
        SelectedSavingsCurrencyConversions = new(FromSelectedCurrencySavingsConversions.Union(ToSelectedCurrencySavingsConversions).OrderBy(x => x.Date));
        SelectedNonSavingsCurrencyConversion = new(FromSelectedCurrencyNonSavingsConversions.Union(ToSelectedCurrencyNonSavingsConversions).OrderBy(x => x.Date));
    }
    private void CheckPercentages()
    {
        if (!PercentageSumEquals100())
        {
            Application.Current.MainPage.DisplayAlert("Invalid Percentages Total", "Category percentages + Savings Goal must add up to 100%", "Ok");
            if (EditMode == false)
            {
                EditMode = true;
            }
        }
        else
        {
            SaveAllSpendingCategories();
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
            newSavings.Percentage = 100;
            SavingsCategoryViewModels.Add(new(newSavings));
        }
    }
    private void SaveAllSpendingCategories()
    {
        if (PercentageSumEquals100())
            SpendingCategoryManager.WriteToJson(SpendingCategoryViewModels.Select(x => x.Category), SavingsCategoryViewModels.Select(x => x.Category));
    }
    private void SaveAllCurrencyConversions()
    {
        CurrencyConversionManager.WriteToJson(CurrencyConversions);
    }
}
