using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MauiApp1.Models;
using MauiApp1.Views;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Windows.Input;

namespace MauiApp1.ViewModels;
public partial class SettingsViewModel : ObservableObject
{
    public ICommand SaveSettingsCommand { get; private set; }

    [ObservableProperty]
    ObservableCollection<string> projectTypes;

    [ObservableProperty]
    ObservableCollection<Agent> agents;

    [ObservableProperty]
    ObservableCollection<string> currencies;

    [ObservableProperty]
    bool welcomeTextVisible;

    [ObservableProperty]
    string typeEntry;

    [ObservableProperty]
    string currencyEntry;

    [ObservableProperty]
    string agentNameEntry;

    [ObservableProperty]
    string agentFeeEntry;

    private ObservableCollection<object> selectedTypes = new();
    private ObservableCollection<object> selectedAgents = new();
    private ObservableCollection<object> selectedCurrencies = new();

    public SettingsViewModel()
    {
        SaveSettingsCommand = new AsyncRelayCommand(SaveSettings);
    }

    public ObservableCollection<object> SelectedTypes
    {
        get => selectedTypes;

        set => selectedTypes = value;
    }

    public ObservableCollection<object> SelectedAgents
    {
        get => selectedAgents;

        set => selectedAgents = value;
    }

    public ObservableCollection<object> SelectedCurrencies
    {
        get => selectedCurrencies;

        set => selectedCurrencies = value;
    }

    public void CopySettingsFromModel()
    {
        ProjectTypes = new(Settings.ProjectTypes);
        Agents = new(Settings.Agents);
        Currencies = new(Settings.Currencies);
    }

    [RelayCommand]
    public void DeleteTypes()
    {
        var typesToRemove = selectedTypes.Select(x => x.ToString()).ToList();
        foreach (var t in typesToRemove)
        {
            ProjectTypes.Remove(t);
        }
    }

    [RelayCommand]
    public void DeleteCurrencies()
    {
        var currenciesToRemove = selectedCurrencies.Select(x => x.ToString()).ToList();
        foreach (var c in currenciesToRemove)
        {
            Currencies.Remove(c);
        }
    }

    [RelayCommand]
    public void DeleteAgents()
    {
        ObservableCollection<Agent> agentsToRemove = new();
        for (int i = 0; i < selectedAgents.Count; i++)
        {
            if (selectedAgents[i] is Agent agent)
            {
                agentsToRemove.Add(agent);
            }
        }
        Agents = new(Agents.Except(agentsToRemove));
    }

    [RelayCommand]
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

    [RelayCommand]
    public void AddAgent()
    {
        //Check if fee entry is valid %
        if (decimal.TryParse(AgentFeeEntry, out decimal result) && (result > 0 && result < 100))
        {
            bool containsIgnoreCase = Agents.Any(s => s.Name.Equals(AgentNameEntry, StringComparison.OrdinalIgnoreCase) && s.FeeDecimal == result / 100);

            if (!string.IsNullOrWhiteSpace(AgentNameEntry) && !containsIgnoreCase)
            {
                Agents.Add(new(AgentNameEntry, result));
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

        else
        {
            Application.Current.MainPage.DisplayAlert("Invalid Fee Input!", "Values between 0 and 100 only.", "Ok");
            AgentFeeEntry = null;
        }
    }

    [RelayCommand]
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

            Settings.Save(new(ProjectTypes), new(Currencies), new(Agents));
        }
    }
}
