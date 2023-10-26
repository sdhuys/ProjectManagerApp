using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MauiApp1.Models;
using System.Collections.ObjectModel;

namespace MauiApp1.ViewModels;
public partial class SettingsViewModel : ObservableObject
{
    [ObservableProperty]
    ObservableCollection<string> projectTypes;

    [ObservableProperty]
    ObservableCollection<Agent> agents;

    [ObservableProperty]
    ObservableCollection<Agent> agentsIncludingNull;

    [ObservableProperty]
    ObservableCollection<string> currencies;

    [ObservableProperty]
    bool welcomeTextVisible;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddTypeCommand))]
    string typeEntry;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddCurrencyCommand))]
    string currencyEntry;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddAgentCommand))]
    string agentNameEntry;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddAgentCommand))]
    string agentFeeEntry;

    public SettingsViewModel()
    {
        var settings = Settings.LoadFromJson();
        ProjectTypes = new (settings.Item1);
        Currencies = new (settings.Item2);
        AgentsIncludingNull = new(settings.Item3);
        Agents = new (AgentsIncludingNull.Where(x => x != null));
    }

    [RelayCommand]
    public void DeleteType(string type)
    {
        ProjectTypes.Remove(type);
    }

    [RelayCommand]
    public void DeleteCurrency(string currency)
    {
        Currencies.Remove(currency);
    }

    [RelayCommand]
    public void DeleteAgent(Agent agent)
    {
        Agents.Remove(agent);
        AgentsIncludingNull.Remove(agent);
    }

    [RelayCommand(CanExecute = nameof(CanAddType))]
    public void AddType()
    {
        bool containsIgnoreCase = ProjectTypes.Any(s => s.Equals(TypeEntry, StringComparison.OrdinalIgnoreCase));

        if (!String.IsNullOrWhiteSpace(TypeEntry) && !containsIgnoreCase)
        {
            ProjectTypes.Add(TypeEntry);
            TypeEntry = null;
        }

        else if (containsIgnoreCase)
        {
            Application.Current.MainPage.DisplayAlert("Duplicate entry!", $"{TypeEntry} already added!", "Ok");
        }
    }

    partial void OnAgentFeeEntryChanged(string oldValue, string newValue)
    {
        if (string.IsNullOrWhiteSpace(newValue)) return;

        if (!decimal.TryParse(newValue, out decimal result) || result < 0 || result > 100)
        {
            AgentFeeEntry = oldValue;
        }
    }

    [RelayCommand(CanExecute = nameof(CanAddAgent))]
    public void AddAgent()
    {
        var agentFeeDecimal = decimal.Parse(AgentFeeEntry) / 100;

        bool containsIgnoreCase = Agents.Any(s => s!= null && s.Name.Equals(AgentNameEntry, StringComparison.OrdinalIgnoreCase) && s.FeeDecimal == agentFeeDecimal);

        if (!containsIgnoreCase)
        {
            Agent newAgent = new( AgentNameEntry, agentFeeDecimal);
            Agents.Add(newAgent);
            AgentsIncludingNull.Add(newAgent);
            AgentNameEntry = null;
            AgentFeeEntry = null;
        }

        else if (containsIgnoreCase)
        {
            Application.Current.MainPage.DisplayAlert("Duplicate Entry!", $"{AgentNameEntry} with {AgentFeeEntry}% already added!", "Ok");
            AgentNameEntry = null;
            AgentFeeEntry = null;
        }
    }
 
    [RelayCommand(CanExecute = nameof(CanAddCurrency))]
    public void AddCurrency()
    {
        bool containsIgnoreCase = Currencies.Any(s => s.Equals(CurrencyEntry, StringComparison.OrdinalIgnoreCase));

        if (!String.IsNullOrWhiteSpace(CurrencyEntry) && !containsIgnoreCase)
        {
            Currencies.Add(CurrencyEntry);
            CurrencyEntry = null;
        }

        else if (containsIgnoreCase)
        {
            Application.Current.MainPage.DisplayAlert("Duplicate entry!", $"{CurrencyEntry} already added!", "Ok");
        }
    }

    private bool CanAddAgent()
    {
        return !(String.IsNullOrWhiteSpace(AgentNameEntry) || String.IsNullOrWhiteSpace(AgentFeeEntry));
    }

    private bool CanAddCurrency()
    {
        return !(String.IsNullOrWhiteSpace(CurrencyEntry));
    }

    private bool CanAddType()
    {
        return !(String.IsNullOrWhiteSpace(TypeEntry));
    }

    [RelayCommand]
    private async Task SaveSettings()
    {
        if (ProjectTypes?.Any() != true || Currencies?.Any() != true)
        {
            await Application.Current.MainPage.DisplayAlert("Alert", "Make sure to set at least one currency and one project type!", "Ok");
        }

        else
        {
            // After first time saving settings load appshell
            if (!Settings.AreSet)
            {
                ((App)Application.Current).SetMainPageToAppShell();
            }

            else
            {
                Shell.Current.CurrentItem = Shell.Current.Items.ElementAt(0);
            }

            //Insert null to displey "None" as picker option on ProjectDetailsPage
            if (!Agents.Contains(null))
                Agents.Insert(0, null);

            Settings.Save(new(ProjectTypes), new(Currencies), new(Agents));
        }
    }
}
