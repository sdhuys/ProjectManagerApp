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
        List<string> typesList;

        [ObservableProperty]
        string description;

        [ObservableProperty]
        DateTime date = DateTime.Now;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(SaveNewProjectCommand))]
        string currency;

        [ObservableProperty]
        List<string> currencyList;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(SaveNewProjectCommand))]
        string fee;

        [ObservableProperty]
        ObservableCollection<Expense> expenses;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(AgentIsSelected))]
        Agent agent;

        [ObservableProperty]
        List<Agent> agentList;

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
        bool paid;

        [ObservableProperty]
        ObservableCollection<Payment> payments;

        public CultureInfo CurrentCulture => CultureInfo.CurrentCulture;


        public NewProjectViewModel()
        {
            AgentList = new(Settings.Agents);
            //Add null to display "None" as first picker option
            AgentList.Insert(0, null);
            TypesList = Settings.ProjectTypes;
            CurrencyList = Settings.Currencies;
            Expenses = new();
            Payments = new();
        }

        public bool AgentIsSelected
        {
            get
            {
                if (Agent == null) HasCustomAgencyFee = false;
                return Agent != null;
            }
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

            Project project = new(Client, Type, Description, Date, Currency, decimal.Parse(Fee), Agent, agencyFeeDecimal, Expenses.ToList(), Payments.ToList(), Paid);
            Projects.Add(project);
            ProjectManager.SaveProjects(Projects);
            await Shell.Current.GoToAsync("..");
        }

        private bool CanAddExpense()
        {
            return (!(NewExpenseName  == null || NewExpenseValue == null));
        }

        private bool CanSaveProject()
        {
            return (!(Client == null || Fee == null || Currency == null));
        }
    }
}
