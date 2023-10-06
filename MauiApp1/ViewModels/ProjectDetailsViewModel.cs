using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MauiApp1.Models;
using System.Collections.ObjectModel;
using System.Globalization;

namespace MauiApp1.ViewModels;
public partial class ProjectDetailsViewModel : ObservableObject, IQueryAttributable
{
    private ObservableCollection<Project> Projects { get; set; }

    private Project selectedProject;

    public bool EditMode => selectedProject != null;
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
        Date = DateTime.Now;
        NewPaymentDate = DateTime.Now;
    }

    private void LoadSelectedProjectDetails()
    {
        Client = selectedProject.Client;
        Type = selectedProject.Type;
        Description = selectedProject.Description;
        Date = selectedProject.Date;
        Currency = selectedProject.Currency;
        Fee = selectedProject.Fee.ToString();
        Agent = selectedProject.Agent;
        HasCustomAgencyFee = Agent != null && selectedProject.AgencyFeeDecimal != Agent.FeeDecimal;
        CustomAgencyFeePercent = HasCustomAgencyFee ? (selectedProject.AgencyFeeDecimal * 100m).ToString() : "0";
        Status = selectedProject.Status;
        Expenses = new(selectedProject.Expenses);
        Payments = new(selectedProject.Payments);
    }

    partial void OnFeeChanged(string oldValue, string newValue)
    {
        if (string.IsNullOrEmpty(newValue)) 
            return;

        if (!decimal.TryParse(newValue, out decimal result))
            Fee = oldValue; 
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
        {
            CustomAgencyFeePercent = oldValue;
        }
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
        Payments.Add(new(Convert.ToDecimal(NewPaymentAmount), NewPaymentDate));
        NewPaymentAmount = null;
        NewPaymentDate = DateTime.Now;
    }

    [RelayCommand]
    void DeletePayment(Payment payment)
    {
        Payments.Remove(payment);
    }

    [RelayCommand]
    async Task Cancel()
    {
        await Shell.Current.GoToAsync("..");
    }

    [RelayCommand(CanExecute = nameof(CanSaveProject))]
    async Task SaveNewOrEditProject()
    {
        var agencyFeeDecimal = Agent == null ? 0 : HasCustomAgencyFee ? decimal.Parse(CustomAgencyFeePercent) / 100m : Agent.FeeDecimal;
        RelativeExpenseCalculator.SetRelativeExpensesAmounts(Expenses, decimal.Parse(Fee), agencyFeeDecimal);

        //Save new project
        if (!EditMode)
        {
            Project project = new(Client, Type, Description, Date, Currency, decimal.Parse(Fee), Agent, agencyFeeDecimal, Expenses.ToList(), Payments.ToList(), Status);
            Projects.Add(project);
        }

        //Save edits
        else
        {
            selectedProject.Client = Client;
            selectedProject.Type = Type;
            selectedProject.Description = Description;
            selectedProject.Date = Date;
            selectedProject.Currency = Currency;
            selectedProject.Fee = decimal.Parse(Fee);
            selectedProject.AgencyFeeDecimal = agencyFeeDecimal;
            selectedProject.Expenses = Expenses.ToList();
            selectedProject.Payments = Payments.ToList();
            selectedProject.Status = Status;
        }

        ProjectManager.SaveProjects(Projects);
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
            Projects = query["projects"] as ObservableCollection<Project>;
            Expenses = new();
            Payments = new();
        }

        if (query.ContainsKey("selectedProject"))
        {
            selectedProject = query["selectedProject"] as Project;
            LoadSelectedProjectDetails();
        }
    }
}
