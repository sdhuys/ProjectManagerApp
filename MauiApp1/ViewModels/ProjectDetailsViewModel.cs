using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MauiApp1.Models;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;

namespace MauiApp1.ViewModels;
public partial class ProjectDetailsViewModel : ObservableObject, IQueryAttributable
{
    private SettingsViewModel settingsViewModel;
    private ObservableCollection<ProjectViewModel> Projects { get; set; }

    private ProjectViewModel selectedProjectVM;

    public bool EditMode => selectedProjectVM != null;
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
    public ProjectDetailsViewModel(SettingsViewModel settings)
    {
        Expenses = new();
        Payments = new();

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
            if (!AgentList.Any(x => x.Agent == selectedProjectVM.Agent))
            {
                AgentWrapper wrapper = new(selectedProjectVM.Agent);
                AgentList = new(AgentList)
                {
                    wrapper
                };
            }

            if (!TypesList.Contains(selectedProjectVM.Type))
            {
                TypesList = new(TypesList)
                {
                    selectedProjectVM.Type
                };
            }

            if (!CurrencyList.Contains(selectedProjectVM.Currency))
            {
                CurrencyList = new(CurrencyList)
                {
                    selectedProjectVM.Currency
                };
            }

            LoadSelectedProjectDetails();
        }
    }

    private void LoadSelectedProjectDetails()
    {
        AgentWrapper = AgentList.FirstOrDefault(x => x.Agent == selectedProjectVM.Agent);
        Client = selectedProjectVM.Client;
        Type = selectedProjectVM.Type;
        Description = selectedProjectVM.Description;
        Date = selectedProjectVM.Date.Date;
        Currency = selectedProjectVM.Currency;
        Fee = selectedProjectVM.Fee.ToString();
        IsVatIncluded = selectedProjectVM.IsVatIncluded;
        VatRatePercent = (selectedProjectVM.VatRateDecimal * 100m).ToString();
        HasCustomAgencyFee = AgentWrapper != null && Agent != null && selectedProjectVM.AgencyFeeDecimal != Agent.FeeDecimal;
        CustomAgencyFeePercent = HasCustomAgencyFee ? (selectedProjectVM.AgencyFeeDecimal * 100m).ToString() : "0";
        Status = selectedProjectVM.Status;
        Expenses = new(selectedProjectVM.Expenses);
        Payments = new(selectedProjectVM.Payments);
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

        if (!decimal.TryParse(newValue, out decimal result))
        {
            Fee = oldValue;
            return;
        }
        CalculateRelativeExpenseAmounts();
    }

    partial void OnVatRatePercentChanged(string oldValue, string newValue)
    {
        if (string.IsNullOrEmpty(newValue))
        {
            CalculateRelativeExpenseAmounts();
            return;
        }

        if (!decimal.TryParse(newValue, out decimal result) || result < 0 || result > 100)
        {
            VatRatePercent = oldValue;
            return;
        }
        CalculateRelativeExpenseAmounts();
    }

    partial void OnNewExpenseValueChanged(string oldValue, string newValue)
    {
        if (string.IsNullOrEmpty(newValue))
            return;

        if (!decimal.TryParse(newValue, out decimal result) || (NewExpenseIsRelative && (result < 0 || result > 100)))
            NewExpenseValue = oldValue;
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

        if (!decimal.TryParse(newValue, out decimal result) || result < 0 || result > 100)
        {
            CustomAgencyFeePercent = oldValue;
            return;
        }
        CalculateRelativeExpenseAmounts();
    }

    partial void OnNewPaymentAmountChanged(string oldValue, string newValue)
    {
        if (string.IsNullOrWhiteSpace(newValue)) return;

        if (!decimal.TryParse(newValue, out decimal result))
        {
            NewPaymentAmount = oldValue;
        }
    }

    partial void OnStatusChanged(Project.ProjectStatus value)
    {
        CalculateRelativeExpenseAmounts();
    }

    // Called on agency/fee/expense/payment changes to update UI
    private void CalculateRelativeExpenseAmounts()
    {
        if (!Expenses.Any(x => x.IsRelative))
            return;
        bool isOnGoing = (int)Status < 2 ? true : false;

        // If the project is concluded, base earnings based on payments received
        var projectEarnings = isOnGoing ? decimal.Parse(Fee) : Payments.Sum(x => x.Amount);

        // If earnings are based on payments received, agency fee is irrelevant
        decimal agencyFeeDecimal;
        if (!isOnGoing || AgentWrapper.Agent == null || HasCustomAgencyFee && String.IsNullOrEmpty(CustomAgencyFeePercent))
            agencyFeeDecimal = 0;

        else if (HasCustomAgencyFee && !String.IsNullOrEmpty(CustomAgencyFeePercent))
            agencyFeeDecimal = decimal.Parse(CustomAgencyFeePercent) / 100m;

        else
            agencyFeeDecimal = Agent.FeeDecimal;

        var earningsExcludingVat = (IsVatIncluded && !String.IsNullOrEmpty(VatRatePercent)) ? projectEarnings / (1 + decimal.Parse(VatRatePercent) / 100m) : projectEarnings;
        RelativeExpenseCalculator.SetRelativeExpensesAmounts(Expenses, earningsExcludingVat, agencyFeeDecimal);
    }

    private void SetRelativeExpensesDateToToday()
    {
        foreach (var expense in Expenses.Where(e => e.IsRelative))
        {
            expense.Date = DateTime.Today;
        }
    }

    [RelayCommand(CanExecute = nameof(CanAddExpense))]
    void AddExpense()
    {
        Expenses.Add(new(NewExpenseName, NewExpenseIsRelative, Convert.ToDecimal(NewExpenseValue), NewExpenseDate));
        CalculateRelativeExpenseAmounts();

        NewExpenseIsRelative = false;
        NewExpenseName = null;
        NewExpenseValue = null;
    }

    [RelayCommand]
    void DeleteExpense(ProjectExpense expense)
    {
        Expenses.Remove(expense);
        CalculateRelativeExpenseAmounts();
    }

    [RelayCommand(CanExecute = nameof(CanAddPayment))]
    void AddPayment()
    {
        Payment newPayment;
        if (EditMode)
        {
            newPayment = new Payment(Convert.ToDecimal(NewPaymentAmount), NewPaymentDate.Date, selectedProjectVM.Id);
        }
        else
        {
            newPayment = new Payment(Convert.ToDecimal(NewPaymentAmount), NewPaymentDate.Date);
        }
        Payments.Add(newPayment);

        // Calculate rel expenses if project is not ongoing (=> calculation based on actual payments)
        if ((int)Status > 1)
        {
            CalculateRelativeExpenseAmounts();
        }

        NewPaymentAmount = null;
        NewPaymentDate = DateTime.Today;
    }

    [RelayCommand]
    void DeletePayment(Payment payment)
    {
        Payments.Remove(payment);

        if ((int)Status > 1)
        {
            CalculateRelativeExpenseAmounts();
        }
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

        //Create new projectviewmodel and add to the collection
        if (!EditMode)
        {
            ProjectViewModel projectVM = new(Client, Type, Description, Date, Currency, decimal.Parse(Fee), IsVatIncluded, vatRateDecimal, Agent, agencyFeeDecimal, Expenses.ToList(), Payments.ToList(), Status);
            Projects.Add(projectVM);
        }

        //Save edits
        else
        {
            // If project is being set to Finished/Cancelled
            if ((int)selectedProjectVM.Status < 2 && (int)Status >= 2)
            {
                SetRelativeExpensesDateToToday();
            }

            selectedProjectVM.Client = Client;
            selectedProjectVM.Type = Type;
            selectedProjectVM.Description = Description;
            selectedProjectVM.Date = Date;
            selectedProjectVM.Currency = Currency;
            selectedProjectVM.Fee = decimal.Parse(Fee);
            selectedProjectVM.IsVatIncluded = IsVatIncluded;
            selectedProjectVM.VatRateDecimal = vatRateDecimal;
            selectedProjectVM.Agent = Agent;
            selectedProjectVM.AgencyFeeDecimal = agencyFeeDecimal;
            selectedProjectVM.Expenses = Expenses.ToList();
            selectedProjectVM.Payments = Payments.ToList();
            selectedProjectVM.Status = Status;
        }
        ProjectManager.SaveProjects(Projects.Select(x => x.Project).ToList());
        await Shell.Current.GoToAsync("..");
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
        if (query.ContainsKey("projects"))
        {
            Projects = query["projects"] as ObservableCollection<ProjectViewModel>;
        }

        if (query.ContainsKey("selectedProjectVM"))
        {
            selectedProjectVM = query["selectedProjectVM"] as ProjectViewModel;
        }
    }
}
