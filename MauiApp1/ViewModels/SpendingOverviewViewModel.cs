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

    [ObservableProperty]
    public ObservableCollection<CurrencyConversion> selectedCurrencyConversions;
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
    [NotifyCanExecuteChangedFor(nameof(AddCurrencyConversionCommand))]
    public string newToCurrencyEntry;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddCurrencyConversionCommand))]
    public decimal newToAmountEntry;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddCurrencyConversionCommand))]
    public decimal newFromAmountEntry;

    [ObservableProperty]
    public DateTime newConversionDate = DateTime.Today;

    public SpendingOverviewViewModel(SettingsViewModel settings)
    {
        SpendingCategoryViewModels = new(SpendingCategoryManager.LoadFromJson().Select(x => new SpendingCategoryViewModel(x)));
        CurrencyConversions = new(CurrencyConversionManager.LoadFromJson().OrderBy(x => x.Date));
        SelectedCurrencyConversions = new();
        SelectedCurrencySpendingCategoryViewModels = new();
    }

    public void OnAppearing()
    {
        _currencyList = ProjectManager.AllProjects.Select(p => p.Currency).Distinct().ToList();
        OnPropertyChanged(nameof(CurrencyList));
        SelectedCurrency = CurrencyList.Order().FirstOrDefault();
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
        vm.SetBudget(AdjustedActualProfitForCurrency);
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
            SavingsGoalPercentage = 100;
            EditMode = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanAddCurrencyConversion))]
    public void AddCurrencyConversion()
    {
        var newConversion = new CurrencyConversion(SelectedCurrency, NewToCurrencyEntry, NewFromAmountEntry, NewToAmountEntry, NewConversionDate);

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
        OnPropertyChanged(nameof(AdjustedActualProfitForCurrency));
        SetCategoryTotalBudgets();
        OnPropertyChanged(nameof(AbsoluteSavingsGoal));
        OnPropertyChanged(nameof(ActualSavings));

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
        SelectedCurrencyConversions = new(FromSelectedCurrencyConversions.Union(ToSelectedCurrencyConversions).OrderBy(x => x.Date));
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
