using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MauiApp1.Models;
using System.Collections.ObjectModel;

namespace MauiApp1.ViewModels;

public partial class SpendingOverviewViewModel : ObservableObject
{
    [ObservableProperty]
    public string selectedCurrency;

    private List<string> _currencyList;
    public List<string> CurrencyList => _currencyList;
    public List<string> NonSelectedCurrencies => CurrencyList?.Where(c => c != SelectedCurrency).ToList();
    public List<SpendingCategoryViewModel> SpendingCategoryViewModels { get; set; }
    public ObservableCollection<SpendingCategoryViewModel> SelectedCurrencySpendingCategoryViewModels { get; set; }
    public List<CurrencyConversion> CurrencyConversions { get; set; }
    public IEnumerable<CurrencyConversion> ToSelectedCurrencyConversions => CurrencyConversions.Where(c => c.ToCurrency == SelectedCurrency);
    public IEnumerable<CurrencyConversion> FromSelectedCurrencyConversions => CurrencyConversions.Where(c => c.FromCurrency == SelectedCurrency);
    public ObservableCollection<CurrencyConversion> SelectedCurrencyConversions { get; set; }
    public decimal NetConversionsForCurrency => ToSelectedCurrencyConversions.Sum(x => x.ToAmount) - FromSelectedCurrencyConversions.Sum(x => x.FromAmount);
    public decimal ActualProfitForCurrency => ProjectManager.AllProjects.Where(p => p.Currency == SelectedCurrency)
                                                    .Select(p => new ProjectViewModel(p))
                                                    .Sum(p => p.ActualProfit);
    public decimal AdjustedActualProfitForCurrency => ActualProfitForCurrency + NetConversionsForCurrency;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AbsoluteSavingsGoal))]
    public decimal savingsGoalPercentage;
    public decimal AbsoluteSavingsGoal => SavingsGoalPercentage / 100m * AdjustedActualProfitForCurrency;
    public decimal ActualSavings => AdjustedActualProfitForCurrency - SelectedCurrencySpendingCategoryViewModels.Sum(cat => cat.Expenses.Sum(x => x.Amount));

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EditModeToOpacity))]
    public bool editMode;
    public double EditModeToOpacity => EditMode ? 0 : 1;

    [ObservableProperty]
    public decimal newExpenseEntry;

    [ObservableProperty]
    public DateTime expenseDate = DateTime.Today;

    [ObservableProperty]
    public string newToCurrencyEntry;

    [ObservableProperty]
    public decimal newToAmountEntry;

    [ObservableProperty]
    public decimal newFromAmountEntry;

    [ObservableProperty]
    public DateTime newConversionDate = DateTime.Today;

    public SpendingOverviewViewModel(SettingsViewModel settings)
    {
        SpendingCategoryViewModels = new(SpendingCategoryManager.LoadFromJson().Select(x => new SpendingCategoryViewModel(x)));
        CurrencyConversions = new(CurrencyConversionManager.LoadFromJson());
        SelectedCurrencyConversions = new();
        SelectedCurrencySpendingCategoryViewModels = new();

        /*
        SpendingCategories = new List<SpendingCategoryViewModel>
        {
            new SpendingCategoryViewModel(new SpendingCategory("living expenses", "EUR", 30m, new ObservableCollection<SpendingCategory.Expense> { new(120m, DateTime.Now), new(124m, DateTime.Now), new(120m, DateTime.Now), new(10m, DateTime.Now) })),
            new SpendingCategoryViewModel(new SpendingCategory("living expenses2", "EUR", 40m, new ObservableCollection<SpendingCategory.Expense> { new(120m, DateTime.Now), new(50m, DateTime.Now), new(12m, DateTime.Now), new(120m, DateTime.Now) })),
            new SpendingCategoryViewModel(new SpendingCategory("living expenses3", "EUR", 30m, new ObservableCollection<SpendingCategory.Expense>{ new(12m, DateTime.Now), new(520m, DateTime.Now), new(120m, DateTime.Now), new(20m, DateTime.Now) }))
        };

        SpendingCategoryManager.WriteToJson(SpendingCategories.Select(x => x.Category));
        */
    }

    public void OnAppearing()
    {
        _currencyList = ProjectManager.AllProjects.Select(p => p.Currency).Distinct().ToList();
        OnPropertyChanged(nameof(CurrencyList));

        if (NewCurrencyAddedInProjects())
        {
            AddNewCurrenciesToExpenses();
        }

        SelectedCurrency = CurrencyList.FirstOrDefault();
    }
    public void OnDisappearing()
    {
        SaveAllCurrencyConversions();
        SaveAllSpendingCategories();
    }
    partial void OnSelectedCurrencyChanged(string value)
    {
        if (value == null)
        {
            return;
        }
        PopulateSelectedCurrencyConversions();
        PopulateSelectedCurrencySpendingCategories();
        SavingsGoalPercentage = 100m - SelectedCurrencySpendingCategoryViewModels.Sum(cat => cat.Percentage);
        OnPropertyChanged(nameof(AdjustedActualProfitForCurrency));
        OnPropertyChanged(nameof(AbsoluteSavingsGoal));
        OnPropertyChanged(nameof(ActualSavings));
        OnPropertyChanged(nameof(NonSelectedCurrencies));
        CheckPercentages();
        SetCategoryTotalBudgets();
    }
    partial void OnEditModeChanged(bool value)
    {
        if (!value)
        {
            CheckPercentages();
        }
    }

    [RelayCommand]
    public void AddNewExpenses()
    {
        foreach (var cat in SelectedCurrencySpendingCategoryViewModels)
        {
            cat.AddNewExpense(ExpenseDate);
        }
        OnPropertyChanged(nameof(ActualSavings));

        // MOVE SAVING TO ONDISAPPEARING
        //SaveAll();
    }

    [RelayCommand]
    public void RemoveExpense(SpendingCategory.Expense expense)
    {
        var categoryToRemoveFrom = SelectedCurrencySpendingCategoryViewModels.Where(cat => cat.Expenses.Contains(expense)).FirstOrDefault();
        categoryToRemoveFrom.RemoveExpense(expense);
        OnPropertyChanged(nameof(ActualSavings));
    }

    [RelayCommand]
    public void AddNewCategory()
    {
        SpendingCategory selectedCurrencyCategory = new(string.Empty, SelectedCurrency);
        SpendingCategoryViewModel vm = new(selectedCurrencyCategory);
        SpendingCategoryViewModels.Add(vm);
        SelectedCurrencySpendingCategoryViewModels.Add(vm);
        EditMode = true;
    }

    [RelayCommand]
    public void RemoveCategory(SpendingCategoryViewModel cat)
    {
        SpendingCategoryViewModels.Remove(cat);
        SelectedCurrencySpendingCategoryViewModels.Remove(cat);
        EditMode = true;
    }

    [RelayCommand]
    public void AddCurrencyConversion()
    {
        var newConversion = new CurrencyConversion(SelectedCurrency, NewToCurrencyEntry, NewFromAmountEntry, NewToAmountEntry, NewConversionDate);
        CurrencyConversions.Add(newConversion);
        SelectedCurrencyConversions.Add(newConversion);
        OnPropertyChanged(nameof(AdjustedActualProfitForCurrency));
        SetCategoryTotalBudgets();
        OnPropertyChanged(nameof(AbsoluteSavingsGoal));
        OnPropertyChanged(nameof(ActualSavings));

        NewToAmountEntry = 0;
        NewFromAmountEntry = 0;
        NewConversionDate = DateTime.Today;
    }

    [RelayCommand]
    public void RemoveCurrencyConversion(CurrencyConversion conv)
    {
        CurrencyConversions.Remove(conv);
        SelectedCurrencyConversions.Remove(conv);
        OnPropertyChanged(nameof(AdjustedActualProfitForCurrency));
        SetCategoryTotalBudgets();
        OnPropertyChanged(nameof(AbsoluteSavingsGoal));
        OnPropertyChanged(nameof(ActualSavings));
    }
    private void SetCategoryTotalBudgets()
    {
        foreach (var category in SelectedCurrencySpendingCategoryViewModels)
        {
            category.SetBudget(AdjustedActualProfitForCurrency);
        }
    }
    // Returns true if project has been added/edited with new currency that has not yet been included in SpendingCategories' ExpensePerCurrency dictionary
    private bool NewCurrencyAddedInProjects()
    {
        return SpendingCategoryViewModels.Any() && CurrencyList.Except(SpendingCategoryViewModels.Select(c => c.Currency)).Any();
    }
    // Creates Spending Categories with same names, currencies and percentages for each new currency
    private void AddNewCurrenciesToExpenses()
    {
        if (SpendingCategoryViewModels.Count() == 0)
            return;

        List<SpendingCategoryViewModel> toAdd = new();

        foreach (var currency in CurrencyList.Except(SpendingCategoryViewModels.Select(c => c.Currency).Distinct()))
        {
            foreach (var category in SpendingCategoryViewModels.DistinctBy(x => x.Name))
            {
                var newCategory = new SpendingCategory(category.Name, currency, category.Percentage, new());
                toAdd.Add(new SpendingCategoryViewModel(newCategory));
            }

            foreach (var categoryViewModel in toAdd)
            {
                SpendingCategoryViewModels.Add(categoryViewModel);
            }
            toAdd.Clear();
        }

        //SaveAll();
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
        SelectedCurrencyConversions.Clear();

        foreach (var conv in FromSelectedCurrencyConversions.Union(ToSelectedCurrencyConversions))
        {
            SelectedCurrencyConversions.Add(conv);
        }
    }
    private void CheckPercentages()
    {
        if (!PercentageSumEquals100() && SelectedCurrencySpendingCategoryViewModels.Count > 0)
        {
            Application.Current.MainPage.DisplayAlert("Invalid Percentages Total", "Category percentages + savings goal must add up to 100%", "Ok");
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
        return SelectedCurrencySpendingCategoryViewModels.Sum(x => x.Percentage) + SavingsGoalPercentage == 100m;
    }
    private void SaveAllSpendingCategories()
    {
        if (PercentageSumEquals100())
            SpendingCategoryManager.WriteToJson(SpendingCategoryViewModels.Select(x => x.Category));
    }
    private void SaveAllCurrencyConversions()
    {
        CurrencyConversionManager.WriteToJson(CurrencyConversions);
    }
}
