using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MauiApp1.Models;
using System.Collections.ObjectModel;
using System.Diagnostics;
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
    bool isVAT_Included;

    [ObservableProperty]
    string vatRatePercent;

    [ObservableProperty]
    ObservableCollection<Expense> expenses;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AgentIsSelected))]
    Agent agent;

    [ObservableProperty]
    bool hasCustomAgencyFee;

    [ObservableProperty]
    string customAgencyFeePercent;

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

    public List<Agent> AgentList { get; set; }
    public List<string> TypesList { get; set; }
    public List<string> CurrencyList { get; set; }
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
    public ProjectDetailsViewModel()
    {
        AgentList = Settings.Agents;
        TypesList = Settings.ProjectTypes;
        CurrencyList = Settings.Currencies;
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
        IsVAT_Included = selectedProjectVM.IsVatIncluded;
        VatRatePercent = (selectedProjectVM.VatRateDecimal * 100m).ToString();
        Agent = selectedProjectVM.Agent;
        HasCustomAgencyFee = Agent != null && selectedProjectVM.AgencyFeeDecimal != Agent.FeeDecimal;
        CustomAgencyFeePercent = HasCustomAgencyFee ? (selectedProjectVM.AgencyFeeDecimal * 100m).ToString() : "0";
        Status = selectedProjectVM.Status;
        Expenses = new(selectedProjectVM.Expenses);
        Payments = new(selectedProjectVM.Payments);
    }

    partial void OnAgentChanged(Agent value)
    {
        HasCustomAgencyFee = false;
        CustomAgencyFeePercent = null;
    }

    partial void OnFeeChanged(string oldValue, string newValue)
    {
        if (string.IsNullOrEmpty(newValue))
            return;

        if (!decimal.TryParse(newValue, out decimal result))
            Fee = oldValue;
    }

    partial void OnVatRatePercentChanged(string oldValue, string newValue)
    {
        if (string.IsNullOrEmpty(newValue))
            return;

        if (!decimal.TryParse(newValue, out decimal result) || result < 0 || result > 100)
            VatRatePercent = oldValue;
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

    partial void OnCustomAgencyFeePercentChanged(string oldValue, string newValue)
    {
        if (string.IsNullOrWhiteSpace(newValue)) return;

        if (!decimal.TryParse(newValue, out decimal result) || result < 0 || result > 100)
            CustomAgencyFeePercent = oldValue;
    }

    partial void OnNewPaymentAmountChanged(string oldValue, string newValue)
    {
        if (string.IsNullOrWhiteSpace(newValue)) return;

        if (!decimal.TryParse(newValue, out decimal result))
        {
            NewPaymentAmount = oldValue;
        }
    }

    [RelayCommand(CanExecute = nameof(CanAddExpense))]
    void AddExpense()
    {
        Expenses.Add(new(NewExpenseName, NewExpenseIsRelative, Convert.ToDecimal(NewExpenseValue)));
        NewExpenseIsRelative = false;
        NewExpenseName = null;
        NewExpenseValue = null;
    }

    [RelayCommand]
    void DeleteExpense(Expense expense)
    {
        Expenses.Remove(expense);
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
        RelativeExpenseCalculator.SetRelativeExpensesAmounts(Expenses, decimal.Parse(Fee), agencyFeeDecimal);

        //Create new projectviewmodel and add to the collection
        if (!EditMode)
        {
            ProjectViewModel projectVM = new(Client, Type, Description, Date, Currency, decimal.Parse(Fee), IsVAT_Included, vatRateDecimal, Agent, agencyFeeDecimal, Expenses.ToList(), Payments.ToList(), Status);
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
            selectedProjectVM.IsVatIncluded = IsVAT_Included;
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
        return (!(Client == null || Fee == null || Currency == null));
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
