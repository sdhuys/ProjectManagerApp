using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MauiApp1.Models;
using MauiApp1.StaticHelpers;
using Microcharts;
using SkiaSharp;
using System.Collections.ObjectModel;
using System.Globalization;

namespace MauiApp1.ViewModels;

public partial class PaymentsOverviewViewModel : ObservableObject
{
    private SettingsViewModel _settingsViewModel;

    private IEnumerable<Project> _projects;

    [ObservableProperty]
    DateTime queryStartDate;

    [ObservableProperty]
    DateTime queryEndDate = DateTime.Today;

    public ObservableCollection<object> QueryCurrencies { get; set; }

    [ObservableProperty]
    ObservableCollection<string> currencyList;
    public ObservableCollection<object> QueryTypes { get; set; }

    [ObservableProperty]
    ObservableCollection<string> typeList;
    public ObservableCollection<object> QueryAgents { get; set; }

    [ObservableProperty]
    ObservableCollection<AgentWrapper> agentList;
    public ObservableCollection<CurrencyExpectedPaymentsViewModel> CurrencyExpectedIncome { get; set; }

    public ObservableCollection<CurrencyIncomeDetails> CurrencyIncomeDetailsCollection { get; set; }

    [ObservableProperty]
    bool enableFilters;

    [ObservableProperty]
    bool filterCurrencies;

    [ObservableProperty]
    bool filterTypes;

    [ObservableProperty]
    bool filterAgents;

    [ObservableProperty]
    bool filterDates;
    public CultureInfo CurrentCulture => CultureInfo.CurrentCulture;

    public PaymentsOverviewViewModel(SettingsViewModel settings, ProjectsViewModel projectsViewModel)
    {
        _projects = projectsViewModel.ProjectsToInject;
        _settingsViewModel = settings;
        QueryStartDate =  _projects.Count() > 1 ? _projects.SelectMany(p => p.Payments).Min(x => x.Date) : DateTime.Today;
        GetFilterOptions();
        QueryCurrencies = new();
        QueryTypes = new();
        QueryAgents = new();
        CurrencyIncomeDetailsCollection = new();
        CurrencyExpectedIncome = new();

        EnableFilters = false;
    }

    public void OnPageAppearing()
    {
        GetFilterOptions();
        GetIncomeDataAndCreateCharts();
        CreateCurrencyExpectedEarningsViewModels();
    }

    // Get Currencies, Types and Agents from Settings, and check all projects for currencies, types or agents not included in settings and add.
    private void GetFilterOptions()
    {
        CurrencyList = _settingsViewModel.Currencies;
        TypeList = _settingsViewModel.ProjectTypes;
        AgentList = _settingsViewModel.AgentsIncludingNull;

        var currenciesFromProjects = _projects.Select(x => x.Currency);
        var typesFromProjects = _projects.Select(x => x.Type);
        var agentsFromProjects = from project in _projects
                                 where project.Agent != null
                                 select new AgentWrapper(project.Agent);

        CurrencyList = new(CurrencyList.Union(currenciesFromProjects));
        TypeList = new(TypeList.Union(typesFromProjects));
        AgentList = new(AgentList.UnionBy(agentsFromProjects, x => x.Agent != null ? x.Agent.Name : null));
    }
    //Called inside IncomePage.OnAppearing to make sure changes in projects are accounted for
    [RelayCommand]
    private void GetIncomeDataAndCreateCharts()
    {
        CurrencyIncomeDetailsCollection.Clear();

        var queriedProjects = !EnableFilters ? _projects : GetQueriedProjects();

        // Group of PaymentViewModels by associated Currency, ProjectType, Client and Agent
        var groupedData = queriedProjects
            .GroupBy(project => new { project.Currency, project.Type, project.Client, project.Agent?.Name })
            .Select(group =>
            {
                var startDateToQuery = FilterDates ? QueryStartDate : DateTime.MinValue;
                var endDateToQuery = FilterDates ? QueryEndDate : DateTime.MaxValue;

                IEnumerable<PaymentWrapper> payments = ProjectsQuery.GetProjectPaymentsWithinDates(group, QueryStartDate, QueryEndDate);
                IEnumerable<ProjectExpense> expenses = ProjectsQuery.GetProjectExpensesWithinDates(group, QueryStartDate, QueryEndDate);

                decimal totalIncome = payments.Sum(payment => payment.Amount);
                decimal totalVat = payments.Sum(payment => payment.VatAmount);
                decimal totalExpenses = expenses.Sum(expense => expense.Amount);

                return new
                {
                    Currency = group.Key.Currency,
                    Type = group.Key.Type,
                    Client = group.Key.Client,
                    AgentName = group.Key.Name,
                    TotalIncome = totalIncome,
                    TotalExpenses = totalExpenses,
                    TotalVat = totalVat,
                    TotalProfit = totalIncome - (totalExpenses + totalVat)
                };
            });

        var uniqueCurrencies = queriedProjects.Select(payment => payment.Currency).Distinct();

        // Prepare data for each CurrencyIncomeDetails
        var dataForCharts = new Dictionary<string, Dictionary<string, decimal>>();
        foreach (var currency in uniqueCurrencies)
        {
            dataForCharts.Clear();

            var totalCurrencyData = groupedData.Where(item => item.Currency == currency);
            var totalIncome = totalCurrencyData.Sum(x => x.TotalIncome);
            var totalVat = totalCurrencyData.Sum(x => x.TotalVat);
            var totalProfit = totalCurrencyData.Sum(x => x.TotalProfit);
            var totalExpenses = totalCurrencyData.Sum(x => x.TotalExpenses);

            if (totalIncome == 0 && totalExpenses == 0) return;

            const int maxStringLength = 30;
            const int truncateIndex = 27;
            // Prepare data for charts
            var typeData = groupedData
                .Where(item => item.Currency == currency)
                .GroupBy(item => item.Type)
                .ToDictionary(
                    group => group.Key.Length < maxStringLength ? group.Key : group.Key.Substring(0, truncateIndex) + "...",
                    group => group.Sum(item => item.TotalProfit));

            var clientData = groupedData
                .Where(item => item.Currency == currency)
                .GroupBy(item => item.Client)
                .ToDictionary(
                    group => group.Key.Length < maxStringLength ? group.Key : group.Key.Substring(0, 27) + "...",
                    group => group.Sum(item => item.TotalProfit));

            var agentData = groupedData
                .Where(item => item.Currency == currency)
                .GroupBy(item => item.AgentName)
                .ToDictionary(
                    group => group.Key != null ? (group.Key.Length < maxStringLength ? group.Key : group.Key.Substring(0, truncateIndex) + "...") : "None",
                    group => group.Sum(item => item.TotalProfit));

            dataForCharts.Add($"{currency} Profit Per Project Type", typeData);
            dataForCharts.Add($"{currency} Profit Per Client", clientData);
            dataForCharts.Add($"{currency} Profit Per Agent", agentData);
            var charts = CreateCharts(dataForCharts);

            CurrencyIncomeDetailsCollection.Add(new CurrencyIncomeDetails
            {
                Currency = currency,
                IncomeAmount = totalIncome,
                ProfitAmount = totalProfit,
                VatAmount = totalVat,
                ExpensesAmount = totalExpenses,
                Charts = charts
            });
        }
    }

    private IEnumerable<Project> GetQueriedProjects()
    {
        var currencies = FilterCurrencies ? QueryCurrencies.OfType<string>() : CurrencyList;
        var types = FilterTypes ? QueryTypes.OfType<string>() : TypeList;
        var agents = FilterAgents ? QueryAgents.OfType<AgentWrapper>() : AgentList;

        return ProjectsQuery.ByCurrencyTypeAndAgent(currencies, types, agents.Select(x => x.Agent), _projects);
    }

    private List<ChartWithHeader> CreateCharts(Dictionary<string, Dictionary<string, decimal>> dataForCharts)
    {
        var result = new List<ChartWithHeader>();

        foreach (var chartData in dataForCharts)
        {
            var chartEntries = new List<ChartEntry>();
            chartEntries.AddRange(ConvertToChartEntries(chartData.Value));
            result.Add(new ChartWithHeader
            {
                Chart = new DonutChart
                {
                    Entries = chartEntries,
                    BackgroundColor = SKColors.Beige
                },
                Header = chartData.Key
            }
            );
        }
        return result;
    }
    private static List<ChartEntry> ConvertToChartEntries(Dictionary<string, decimal> data)
    {
        var chartEntries = new List<ChartEntry>();
        Random rnd = new Random();
        foreach (var kvp in data)
        {
            // Show losses without colour label and without piece of chart
            var entryValue = kvp.Value > 0 ? (float)kvp.Value : 0;
            var entryColor = kvp.Value > 0 ? SkiaSharp.SKColor.Parse(String.Format("#{0:X6}", rnd.Next(0x1000000))) : SkiaSharp.SKColors.Transparent;

            chartEntries.Add(new ChartEntry(entryValue)
            {
                Label = kvp.Key,
                ValueLabel = kvp.Value.ToString("N0"),
                Color = entryColor
            });
        }

        return chartEntries;
    }
    private void CreateCurrencyExpectedEarningsViewModels()
    {
        CurrencyExpectedIncome.Clear();

        var projects = _projects.Select(x => new ProjectViewModel(x));

        var activeAndInvoicedProjects = projects.Where(x => x.Status == Project.ProjectStatus.Active || x.Status == Project.ProjectStatus.Invoiced);

        if (!activeAndInvoicedProjects.Any()) return;
        var currencies = projects.Select(x => x.Currency).Distinct();

        foreach (var currency in currencies)
        {
            var expectedPaymentsAmount = activeAndInvoicedProjects
                .Where(x => x.Currency == currency)
                .Select(x => Math.Max(x.TotalExpectedPaymentsAmount - x.PaidAmount, 0))
                .Sum();

            CurrencyExpectedIncome.Add(new CurrencyExpectedPaymentsViewModel
            {
                Currency = currency,
                Amount = expectedPaymentsAmount >= 0 ? expectedPaymentsAmount : 0
            });
        }
    }
    partial void OnEnableFiltersChanged(bool value)
    {
        if (!value)
        {
            FilterCurrencies = false;
            FilterTypes = false;
            FilterAgents = false;
            FilterDates = false;
            QueryAgents.Clear();
            QueryCurrencies.Clear();
            QueryTypes.Clear();
            QueryStartDate = _projects.Count() > 1 ? _projects.Min(x => x.Date) : DateTime.Today;
            QueryEndDate = DateTime.Today;

            GetIncomeDataAndCreateCharts();
        }
    }
    public class ChartWithHeader
    {
        public Chart Chart { get; set; }
        public string Header { get; set; }
    }
    
    // Class representing total payments received, the VAT portion, related expenses and the resulting profit
    // Including charts showing 
    public class CurrencyIncomeDetails
    {
        public string Currency { get; set; }
        public decimal IncomeAmount { get; set; }
        public decimal ProfitAmount { get; set; }
        public decimal VatAmount { get; set; }
        public decimal ExpensesAmount { get; set; }
        public List<ChartWithHeader> Charts { get; set; }
    }

    // Class denoting total expected currency payments [Sum of (Fee - Sum(Payments)) from active and invoiced projects]
    public class CurrencyExpectedPaymentsViewModel
    {
        public string Currency { get; set; }
        public decimal Amount { get; set; }
    }
}
