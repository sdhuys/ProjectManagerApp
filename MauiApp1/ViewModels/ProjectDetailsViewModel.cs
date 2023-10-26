using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MauiApp1.Models;
using System.Collections.ObjectModel;
using System.Globalization;

namespace MauiApp1.ViewModels;
public partial class ProjectDetailsViewModel : ObservableObject, IQueryAttributable
{
    private ObservableCollection<ProjectViewModel> Projects { get; set; }

    private ProjectViewModel selectedProjectVM;

    public bool EditMode => selectedProjectVM != null;
    public string PageTitle => EditMode ? "Edit Project" : "New Project";
    public string CommandTitle => EditMode ? "Save Changes" : "Save New Project";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveNewOrEditProjectCommand))]
    string client;

    [ObservableProperty]
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
    [NotifyPropertyChangedFor(nameof(AgentIsSelected))]
    Agent agent;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveNewOrEditProjectCommand))]
    bool hasCustomAgencyFee;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveNewOrEditProjectCommand))]
    string customAgencyFeePercent;

    [ObservableProperty]
    ObservableCollection<Expense> expenses;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddExpenseCommand))]
    string newExpenseName;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddExpenseCommand))]
    string newExpenseValue;

    [ObservableProperty]
    bool newExpenseIsRelative;

    [ObservableProperty]
    ObservableCollection<Payment> payments;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddPaymentCommand))]
    string newPaymentAmount;

    [ObservableProperty]
    DateTime newPaymentDate;

    [ObservableProperty]
    Project.ProjectStatus status;

    private List<Payment> paymentsToRemoveFromManagerOnCancel = new();
    private List<Payment> paymentsToRemoveFromManagerOnSave = new();

    public ObservableCollection<Agent> AgentList { get; set; }
    public ObservableCollection<string> TypesList { get; set; }
    public ObservableCollection<string> CurrencyList { get; set; }
    public List<Project.ProjectStatus> StatusList { get; set; }
    public CultureInfo CurrentCulture => CultureInfo.CurrentCulture;
    public bool AgentIsSelected
    {
        get
        {
            if (Agent == null)
            {
                HasCustomAgencyFee = false;
                CustomAgencyFeePercent = null;
            }

            return Agent != null;
        }
    }
    public ProjectDetailsViewModel(SettingsViewModel settings)
    {
        AgentList = settings.AgentsIncludingNull;
        TypesList = settings.ProjectTypes;
        CurrencyList = settings.Currencies;
        StatusList = Enum.GetValues(typeof(Project.ProjectStatus)).OfType<Project.ProjectStatus>().ToList();
        Date = DateTime.Today;
        NewPaymentDate = DateTime.Today;
    }

    private void LoadselectedProjectVMDetails()
    {
        Client = selectedProjectVM.Client;
        Type = selectedProjectVM.Type;
        Description = selectedProjectVM.Description;
        Date = selectedProjectVM.Date.Date;
        Currency = selectedProjectVM.Currency;
        Fee = selectedProjectVM.Fee.ToString();
        IsVatIncluded = selectedProjectVM.IsVatIncluded;
        VatRatePercent = (selectedProjectVM.VatRateDecimal * 100m).ToString();
        Agent = selectedProjectVM.Agent;
        HasCustomAgencyFee = Agent != null && selectedProjectVM.AgencyFeeDecimal != Agent.FeeDecimal;
        CustomAgencyFeePercent = HasCustomAgencyFee ? (selectedProjectVM.AgencyFeeDecimal * 100m).ToString() : "0";
        Status = selectedProjectVM.Status;
        Expenses = new(selectedProjectVM.Expenses);
        Payments = new(selectedProjectVM.Payments);
    }

    partial void OnIsVatIncludedChanged(bool value)
    {
        CalculateRelativeExpenseAmounts();
    }

    partial void OnAgentChanged(Agent value)
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

        if (!decimal.TryParse(newValue, out decimal result) || (NewExpenseIsRelative && ( result < 0 || result > 100)))
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

    // Called on agency/fee/expense changes by user to update UI
    private void CalculateRelativeExpenseAmounts()
    {
        if (String.IsNullOrEmpty(Fee))
        {
            RelativeExpenseCalculator.SetRelativeExpensesAmounts(Expenses, 0, 0);
            return;
        }

        decimal agencyFeeDecimal;
        if (Agent == null || HasCustomAgencyFee && String.IsNullOrEmpty(CustomAgencyFeePercent))
            agencyFeeDecimal = 0;

        else if (HasCustomAgencyFee && !String.IsNullOrEmpty(CustomAgencyFeePercent))
            agencyFeeDecimal = decimal.Parse(CustomAgencyFeePercent) / 100m;

        else
            agencyFeeDecimal = Agent.FeeDecimal;

        var feeExcludingVat = (IsVatIncluded && !String.IsNullOrEmpty(VatRatePercent)) ? decimal.Parse(Fee) / (1 + decimal.Parse(VatRatePercent) / 100m) : decimal.Parse(Fee);
        RelativeExpenseCalculator.SetRelativeExpensesAmounts(Expenses, feeExcludingVat, agencyFeeDecimal);
    }

    [RelayCommand(CanExecute = nameof(CanAddExpense))]
    void AddExpense()
    {
        Expenses.Add(new(NewExpenseName, NewExpenseIsRelative, Convert.ToDecimal(NewExpenseValue)));
        CalculateRelativeExpenseAmounts();

        NewExpenseIsRelative = false;
        NewExpenseName = null;
        NewExpenseValue = null;
    }

    [RelayCommand]
    void DeleteExpense(Expense expense)
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
        paymentsToRemoveFromManagerOnCancel.Add(newPayment);

        NewPaymentAmount = null;
        NewPaymentDate = DateTime.Today;
    }

    [RelayCommand]
    void DeletePayment(Payment payment)
    {
        paymentsToRemoveFromManagerOnSave.Add(payment);
        Payments.Remove(payment);
    }

    [RelayCommand]
    async Task Cancel()
    {
        PaymentManager.AllPayments.RemoveAll(paymentsToRemoveFromManagerOnCancel.Contains);
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
        PaymentManager.AllPayments.RemoveAll(paymentsToRemoveFromManagerOnSave.Contains);
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
        return !(String.IsNullOrEmpty(Client) || String.IsNullOrEmpty(Fee) || String.IsNullOrEmpty(Currency) || (HasCustomAgencyFee && String.IsNullOrEmpty(CustomAgencyFeePercent)));
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.ContainsKey("projects"))
        {
            Projects = query["projects"] as ObservableCollection<ProjectViewModel>;
            Expenses = new();
            Payments = new();
        }

        if (query.ContainsKey("selectedProjectVM"))
        {
            selectedProjectVM = query["selectedProjectVM"] as ProjectViewModel;
            LoadselectedProjectVMDetails();
        }
    }
}
