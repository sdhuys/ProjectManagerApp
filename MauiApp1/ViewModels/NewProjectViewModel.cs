using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MauiApp1.Models;
using System.Collections.ObjectModel;
using System.Globalization;

namespace MauiApp1.ViewModels
{
    [QueryProperty("projects", "projects")]
    public partial class NewProjectViewModel : ObservableObject, IQueryAttributable
    {
        ObservableCollection<Project> Projects { get; set; }

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(SaveNewProjectCommand))]
        string client;

        [ObservableProperty]
        string type;

        [ObservableProperty]
        string description;

        [ObservableProperty]
        DateTime date;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(SaveNewProjectCommand))]
        string currency;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(SaveNewProjectCommand))]
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
        public NewProjectViewModel()
        {
            //Create new list and insert null to display "None" as first picker option
            AgentList = new(Settings.Agents);
            AgentList.Insert(0, null);

            TypesList = Settings.ProjectTypes;
            CurrencyList = Settings.Currencies;
            StatusList = Enum.GetValues(typeof(Project.ProjectStatus)).OfType<Project.ProjectStatus>().ToList();
            Expenses = new();
            Payments = new();
            Date = DateTime.Now;
            NewPaymentDate = DateTime.Now;
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
        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            Projects = query["projects"] as ObservableCollection<Project>;
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
        async Task SaveNewProject()
        {
            var agencyFeeDecimal = Agent == null ? 0 : HasCustomAgencyFee ? decimal.Parse(CustomAgencyFeePercent) / 100m : Agent.FeeDecimal;
            RelativeExpenseCalculator.SetRelativeExpensesAmounts(Expenses, decimal.Parse(Fee), agencyFeeDecimal);

            Project project = new(Client, Type, Description, Date, Currency, decimal.Parse(Fee), Agent, agencyFeeDecimal, Expenses.ToList(), Payments.ToList(), Status);
            Projects.Add(project);
            ProjectManager.SaveProjects(Projects);
            await Shell.Current.GoToAsync("..");
        }

        private bool CanAddExpense()
        {
            return (!(NewExpenseName  == null || NewExpenseValue == null));
        }
        private bool CanAddPayment()
        {
            return NewPaymentAmount != null;
        }

        private bool CanSaveProject()
        {
            return (!(Client == null || Fee == null || Currency == null));
        }
    }
}
