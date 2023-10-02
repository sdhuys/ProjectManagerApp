using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MauiApp1.Models;
using System.Collections.ObjectModel;

namespace MauiApp1.ViewModels
{
    [QueryProperty("projects", "projects")]
    public partial class NewProjectViewModel : ObservableObject, IQueryAttributable
    {
        [ObservableProperty]
        string client;

        [ObservableProperty]
        string type;

        [ObservableProperty]
        List<string> typesList = Settings.ProjectTypes;

        [ObservableProperty]
        string description;

        [ObservableProperty]
        DateTime date;

        [ObservableProperty]
        string currency;

        [ObservableProperty]
        List<string> currencyList = Settings.Currencies;

        [ObservableProperty]
        decimal fee;

        [ObservableProperty]
        ObservableCollection<Expense> expenses = new();

        [ObservableProperty]
        bool paid;

        [ObservableProperty]
        Agent agent;

        [ObservableProperty]
        List<Agent> agentList = Settings.Agents;

        [ObservableProperty]
        bool hasCustomAgencyFee;

        [ObservableProperty]
        decimal customAgencyFeePercent;

        [ObservableProperty]
        private string expenseNameEntry;

        [ObservableProperty]
        private string expenseValueEntry;

        [ObservableProperty]
        private bool expenseIsRelative;

        ObservableCollection<Project> Projects { get; set; }

        public NewProjectViewModel()
        {
            Expenses = new ObservableCollection<Expense> { new("Stijn", true, 25), new Expense("brush", false, 10m) };
        }

        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            Projects = query["projects"] as ObservableCollection<Project>;
            OnPropertyChanged(nameof(Projects));
        }

        [RelayCommand]
        void AddExpense()
        {
            Expenses.Add(new(ExpenseNameEntry, ExpenseIsRelative, Convert.ToDecimal(ExpenseValueEntry)));
        }

        [RelayCommand]
        async Task Cancel()
        {
            await Shell.Current.GoToAsync("..");
        }

        [RelayCommand]
        async Task SaveNewProject()
        {
            var agencyFeeDecimal = Agent == null ? 0 : HasCustomAgencyFee ? CustomAgencyFeePercent / 100m : Agent.FeeDecimal;
            RelativeExpenseCalculator.SetRelativeExpensesAmounts(Expenses, Fee, agencyFeeDecimal);

            Project project = new(Client, Type, Description, Currency, Fee, Agent, agencyFeeDecimal, Expenses.ToList(), Paid);
            Projects.Add(project);
            ProjectManager.SaveProjects(Projects);
            await Shell.Current.GoToAsync("..");
        }
    }
}
