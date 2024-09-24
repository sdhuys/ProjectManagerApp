using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MauiApp1.Models;
using MauiApp1.StaticHelpers;
using System.Collections.ObjectModel;
using System.Globalization;

namespace MauiApp1.ViewModels;
public partial class ProjectDetailsViewModel : ObservableObject, IQueryAttributable
{
    private SettingsViewModel settingsViewModel;

    public ProjectViewModel SelectedProjectVM { get; private set; }

    public bool EditMode => SelectedProjectVM != null;
    public string PageTitle => EditMode ? "Edit Project" : "New Project";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveNewOrEditProjectCommand))]
    string client;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveNewOrEditProjectCommand))]
    string type;

    [ObservableProperty]
    string description;

    [ObservableProperty]
    DateTime date;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveNewOrEditProjectCommand))]
    string currency;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveNewOrEditProjectCommand))]
    string fee;

    [ObservableProperty]
    bool isVatIncluded;

    [ObservableProperty]
    string vatRatePercent;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AgentIsSelectedToOpacity))]
    AgentWrapper agentWrapper;

    Agent Agent => AgentWrapper.Agent;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveNewOrEditProjectCommand))]
    bool hasCustomAgencyFee;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveNewOrEditProjectCommand))]
    string customAgencyFeePercent;

    [ObservableProperty]
    ObservableCollection<ProjectExpense> expenses;

    [ObservableProperty]
    ObservableCollection<ProfitSharingExpense> profitSharingExpenses;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddExpenseCommand))]
    string newExpenseName;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddExpenseCommand))]
    string newExpenseValue;

    [ObservableProperty]
    bool newExpenseIsRelative;

    [ObservableProperty]
    DateTime newExpenseDate;

    [ObservableProperty]
    ObservableCollection<Payment> payments;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddPaymentCommand))]
    string newPaymentAmount;

    [ObservableProperty]
    DateTime newPaymentDate;

    [ObservableProperty]
    Project.ProjectStatus status;

    [ObservableProperty]
    public ObservableCollection<AgentWrapper> agentList;

    [ObservableProperty]
    public ObservableCollection<string> typesList;

    [ObservableProperty]
    public ObservableCollection<string> currencyList;
    public List<Project.ProjectStatus> StatusList { get; set; }
    public CultureInfo CurrentCulture => CultureInfo.CurrentCulture;
    public bool AgentIsSelected
    {
        get
        {
            if (AgentWrapper == null || AgentWrapper.Agent == null)
            {
                HasCustomAgencyFee = false;
                CustomAgencyFeePercent = null;
                return false;
            }

            return true;
        }
    }
    public double AgentIsSelectedToOpacity => AgentIsSelected ? 1 : 0;
    public bool HasProfitSharingExpense => ProfitSharingExpenses.Any();
    public ProjectDetailsViewModel(SettingsViewModel settings)
    {
        ProfitSharingExpenses = new();
        settingsViewModel = settings;
        AgentWrapper = settingsViewModel.AgentsIncludingNull.First(x => x.Agent == null);
        AgentList = settingsViewModel.AgentsIncludingNull;
        TypesList = settingsViewModel.ProjectTypes;
        CurrencyList = settingsViewModel.Currencies;

        StatusList = Enum.GetValues(typeof(Project.ProjectStatus)).OfType<Project.ProjectStatus>().ToList();
        Date = DateTime.Today;
        NewPaymentDate = DateTime.Today;
        NewExpenseDate = DateTime.Today;
    }

    public void OnPageAppearing()
    {
        // Add current project details to picker collections if they've been removed from settings (create copies so they don't get added to settings)
        // Makes sure deleted Agents/Types/Currencies aren't lost on project edit page
        // and sets them as current Agent/Type/Currency
        if (EditMode)
        {
            if (!AgentList.Any(x => x.Agent == SelectedProjectVM.Agent))
            {
                AgentWrapper wrapper = new(SelectedProjectVM.Agent);
                AgentList = new(AgentList)
                {
                    wrapper
                };
            }

            if (!TypesList.Contains(SelectedProjectVM.Type))
            {
                TypesList = new(TypesList)
                {
                    SelectedProjectVM.Type
                };
            }

            if (!CurrencyList.Contains(SelectedProjectVM.Currency))
            {
                CurrencyList = new(CurrencyList)
                {
                    SelectedProjectVM.Currency
                };
            }

            LoadSelectedProjectDetails();
        }
    }

    private void LoadSelectedProjectDetails()
    {
        AgentWrapper = AgentList.FirstOrDefault(x => x.Agent == SelectedProjectVM.Agent);
        Client = SelectedProjectVM.Client;
        Type = SelectedProjectVM.Type;
        Description = SelectedProjectVM.Description;
        Date = SelectedProjectVM.Date.Date;
        Currency = SelectedProjectVM.Currency;
        Fee = SelectedProjectVM.Fee.ToString();
        IsVatIncluded = SelectedProjectVM.IsVatIncluded;
        VatRatePercent = (SelectedProjectVM.VatRateDecimal * 100m).ToString();
        HasCustomAgencyFee = AgentWrapper != null && Agent != null && SelectedProjectVM.AgencyFeeDecimal != Agent.FeeDecimal;
        CustomAgencyFeePercent = HasCustomAgencyFee ? (SelectedProjectVM.AgencyFeeDecimal * 100m).ToString() : "0";
        Status = SelectedProjectVM.Status;

        Expenses = new(SelectedProjectVM.Expenses.Where(x => !x.IsRelative));
        ProfitSharingExpenses = new();
        foreach (var expense in SelectedProjectVM.Expenses.OfType<ProfitSharingExpense>())
        {
            // Create deep copies of ProfitSharingExpenses, necessary due to direct editing of Date and IsPaid properties on page
            ProfitSharingExpenses.Add(new ProfitSharingExpense(expense as ProfitSharingExpense));
        }

        Payments = new(SelectedProjectVM.Payments);
        OnPropertyChanged(nameof(HasProfitSharingExpense));
    }

    partial void OnIsVatIncludedChanged(bool value)
    {
        CalculateRelativeExpenseAmounts();
    }

    partial void OnAgentWrapperChanged(AgentWrapper value)
    {
        HasCustomAgencyFee = false;
        CustomAgencyFeePercent = null;
        CalculateRelativeExpenseAmounts();
    }

    partial void OnFeeChanged(string oldValue, string newValue)
    {
        if (string.IsNullOrEmpty(newValue))
        {
            CalculateRelativeExpenseAmounts();
            return;
        }

        newValue = newValue.Replace('.', ',');

        if (!decimal.TryParse(newValue, out decimal result))
        {
            Fee = oldValue;
            return;
        }
        CalculateRelativeExpenseAmounts();
        Fee = newValue;
    }

    partial void OnVatRatePercentChanged(string oldValue, string newValue)
    {
        if (string.IsNullOrEmpty(newValue))
        {
            CalculateRelativeExpenseAmounts();
            return;
        }

        newValue = newValue.Replace('.', ',');

        if (!decimal.TryParse(newValue, out decimal result) || result < 0 || result > 100)
        {
            VatRatePercent = oldValue;
            return;
        }
        CalculateRelativeExpenseAmounts();
        VatRatePercent = newValue;
    }

    partial void OnNewExpenseValueChanged(string oldValue, string newValue)
    {
        if (string.IsNullOrEmpty(newValue))
            return;

        newValue = newValue.Replace('.', ',');

        if (!decimal.TryParse(newValue, out decimal result) || (NewExpenseIsRelative && (result < 0 || result > 100)))
        {
            NewExpenseValue = oldValue;
            return;
        }
        NewExpenseValue = newValue;
    }

    partial void OnNewExpenseIsRelativeChanged(bool value)
    {
        if (value && decimal.TryParse(NewExpenseValue, out decimal result) && result > 100)
        {
            NewExpenseValue = null;
        }
    }

    partial void OnHasCustomAgencyFeeChanged(bool value)
    {
        CalculateRelativeExpenseAmounts();
    }

    partial void OnCustomAgencyFeePercentChanged(string oldValue, string newValue)
    {
        if (string.IsNullOrWhiteSpace(newValue))
        {
            CalculateRelativeExpenseAmounts();
            return;
        }
        newValue = newValue.Replace('.', ',');

        if (!decimal.TryParse(newValue, out decimal result) || result < 0 || result > 100)
        {
            CustomAgencyFeePercent = oldValue;
            return;
        }
        CustomAgencyFeePercent = newValue;
        CalculateRelativeExpenseAmounts();
    }

    partial void OnNewPaymentAmountChanged(string oldValue, string newValue)
    {
        if (string.IsNullOrWhiteSpace(newValue)) return;

        newValue = newValue.Replace('.', ',');

        if (!decimal.TryParse(newValue, out decimal result))
        {
            NewPaymentAmount = oldValue;
            return;
        }
        NewPaymentAmount = newValue;
    }

    partial void OnStatusChanged(Project.ProjectStatus value)
    {
        CalculateRelativeExpenseAmounts();
    }

    // Called on agency/fee/expense/payment changes to update UI
    private void CalculateRelativeExpenseAmounts()
    {
        if (ProfitSharingExpenses == null || ProfitSharingExpenses.Count == 0)
            return;

        var expectedFee = decimal.TryParse(Fee, out decimal result) ? result : 0;
        var actualFee = Payments.Sum(x => x.Amount);

        decimal agencyFeeDecimal;
        if (AgentWrapper.Agent == null || (HasCustomAgencyFee && String.IsNullOrEmpty(CustomAgencyFeePercent)))
            agencyFeeDecimal = 0;

        else if (HasCustomAgencyFee && !String.IsNullOrEmpty(CustomAgencyFeePercent))
            agencyFeeDecimal = decimal.Parse(CustomAgencyFeePercent) / 100m;

        else
            agencyFeeDecimal = Agent.FeeDecimal;

        var expectedEarningsExcludingVat = (IsVatIncluded && !String.IsNullOrEmpty(VatRatePercent)) ? expectedFee / (1 + decimal.Parse(VatRatePercent) / 100m) : expectedFee;
        var actualEarningsExcludingVat = !String.IsNullOrEmpty(VatRatePercent) ? actualFee / (1 + decimal.Parse(VatRatePercent) / 100m) : actualFee;
        RelativeExpenseCalculator.SetRelativeExpensesAmounts(Expenses, ProfitSharingExpenses, expectedEarningsExcludingVat, agencyFeeDecimal, actualEarningsExcludingVat);
    }

    [RelayCommand(CanExecute = nameof(CanAddExpense))]
    void AddExpense()
    {
        if (NewExpenseIsRelative)
        {
            var newExpense = new ProfitSharingExpense(NewExpenseName, Convert.ToDecimal(NewExpenseValue) / 100m, NewExpenseDate);

            if (ProfitSharingExpenses.Any(x => x.Date > newExpense.Date))
            {
                var index = ProfitSharingExpenses.Where(x => x.Date < newExpense.Date).Count();
                ProfitSharingExpenses.Insert(index, newExpense);
            }
            else
            {
                ProfitSharingExpenses.Add(newExpense);
            }
            OnPropertyChanged(nameof(HasProfitSharingExpense));
        }
        else
        {
            ProjectExpense newExpense = new(NewExpenseName, Convert.ToDecimal(NewExpenseValue), NewExpenseDate);

            if (Expenses.Any(x => x.Date > newExpense.Date))
            {
                var index = Expenses.Where(x => x.Date < newExpense.Date).Count();
                Expenses.Insert(index, newExpense);
            }
            else
            {
                Expenses.Add(newExpense);
            }
        }


        CalculateRelativeExpenseAmounts();

        NewExpenseIsRelative = false;
        NewExpenseName = null;
        NewExpenseValue = null;
    }

    [RelayCommand]
    void DeleteExpense(ProjectExpense expense)
    {
        if (expense is ProfitSharingExpense)
        {
            ProfitSharingExpenses.Remove(expense as ProfitSharingExpense);
            OnPropertyChanged(nameof(HasProfitSharingExpense));
        }
        else
        {
            Expenses.Remove(expense);
        }
        CalculateRelativeExpenseAmounts();
    }

    [RelayCommand(CanExecute = nameof(CanAddPayment))]
    void AddPayment()
    {
        Payment newPayment;

        newPayment = EditMode ? new Payment(Convert.ToDecimal(NewPaymentAmount), NewPaymentDate.Date, SelectedProjectVM.Id)
                              : new Payment(Convert.ToDecimal(NewPaymentAmount), NewPaymentDate.Date);

        if (Payments.Any(x => x.Date > newPayment.Date))
        {
            var index = Payments.Where(x => x.Date < newPayment.Date).Count();
            Payments.Insert(index, newPayment);
        }
        else
        {
            Payments.Add(newPayment);
        }

        CalculateRelativeExpenseAmounts();
        NewPaymentAmount = null;
        NewPaymentDate = DateTime.Today;
    }

    [RelayCommand]
    void DeletePayment(Payment payment)
    {
        Payments.Remove(payment);
        CalculateRelativeExpenseAmounts();
    }

    [RelayCommand]
    async Task Cancel()
    {
        await Shell.Current.GoToAsync("..");
    }

    [RelayCommand(CanExecute = nameof(CanSaveProject))]
    async Task SaveNewOrEditProject()
    {
        var vatRateDecimal = VatRatePercent == null ? 0 : decimal.Parse(VatRatePercent) / 100m;
        var agencyFeeDecimal = Agent == null ? 0 : HasCustomAgencyFee ? decimal.Parse(CustomAgencyFeePercent) / 100m : Agent.FeeDecimal;
        List<ProjectExpense> allExpenses = Expenses.Union(ProfitSharingExpenses).OrderBy(x => x.Date).ToList();

        //Create new projectviewmodel and add to the collection
        if (!EditMode)
        {
            Project newProject = new(Client, Type, Description, Date, Currency, decimal.Parse(Fee), IsVatIncluded, vatRateDecimal, Agent, agencyFeeDecimal, allExpenses, Payments.ToList(), Status);
            ProjectViewModel projectVM = new(newProject);
            await Shell.Current.GoToAsync("..", new Dictionary<string, object>
            {
                ["projectVM"] = projectVM,
            });
        }

        //Save edits
        else
        {
            SelectedProjectVM.Client = Client;
            SelectedProjectVM.Type = Type;
            SelectedProjectVM.Description = Description;
            SelectedProjectVM.Date = Date;
            SelectedProjectVM.Currency = Currency;
            SelectedProjectVM.Fee = decimal.Parse(Fee);
            SelectedProjectVM.IsVatIncluded = IsVatIncluded;
            SelectedProjectVM.VatRateDecimal = vatRateDecimal;
            SelectedProjectVM.Agent = Agent;
            SelectedProjectVM.AgencyFeeDecimal = agencyFeeDecimal;
            SelectedProjectVM.Expenses = allExpenses;
            SelectedProjectVM.Payments = Payments.ToList();
            SelectedProjectVM.Status = Status;

            await Shell.Current.GoToAsync("..", new Dictionary<string, object>
            {
                ["projectVM"] = SelectedProjectVM,
            });
        }
    }

    private bool CanAddExpense()
    {
        return (!(String.IsNullOrWhiteSpace(NewExpenseName) || String.IsNullOrWhiteSpace(NewExpenseValue)));
    }
    private bool CanAddPayment()
    {
        return !String.IsNullOrWhiteSpace(NewPaymentAmount);
    }
    private bool CanSaveProject()
    {
        return !(String.IsNullOrEmpty(Client) || String.IsNullOrEmpty(Fee) || String.IsNullOrEmpty(Type) || String.IsNullOrEmpty(Currency) || (HasCustomAgencyFee && String.IsNullOrEmpty(CustomAgencyFeePercent)));
    }
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.ContainsKey("selectedProjectVM"))
        {
            SelectedProjectVM = query["selectedProjectVM"] as ProjectViewModel;
        }
    }
}
