using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MauiApp1.Models;
using Microcharts;
using SkiaSharp;
using System.Collections.ObjectModel;
using System.Globalization;

namespace MauiApp1.ViewModels;

public partial class PaymentsOverviewViewModel : ObservableObject
{
    [ObservableProperty]
    DateTime queryStartDate = ProjectManager.AllProjects.Min(x => x.Date);

    [ObservableProperty]
    DateTime queryEndDate = DateTime.Today;

    public ObservableCollection<object> QueryCurrencies { get; set; }
    public ObservableCollection<string> CurrencyList { get; set; }
    public ObservableCollection<object> QueryTypes { get; set; }
    public ObservableCollection<string> TypeList { get; set; }
    public ObservableCollection<object> QueryAgents { get; set; }
    public ObservableCollection<AgentWrapper> AgentList { get; set; }
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

    public PaymentsOverviewViewModel(SettingsViewModel settings)
    {
        CurrencyList = settings.Currencies;
        QueryCurrencies = new();
        TypeList = settings.ProjectTypes;
        QueryTypes = new();
        AgentList = settings.AgentsIncludingNull;
        QueryAgents = new();
        CurrencyIncomeDetailsCollection = new();
        CurrencyExpectedIncome = new();

        EnableFilters = false;
    }

    //Called inside IncomePage.OnAppearing to make sure changes in projects are accounted for
    [RelayCommand]
    public void ApplyFilters()
    {
        CurrencyIncomeDetailsCollection.Clear();

        var queriedPayments = !EnableFilters ? PaymentManager.AllPayments.Select(p => new PaymentViewModel(p)) : GetQueriedPayments();

        // Group by Currency, Type, and Client
        var groupedData = queriedPayments
            .GroupBy(payment => new { payment.Currency, payment.Type, payment.Client })
            .Select(group => new
            {
                Currency = group.Key.Currency,
                Type = group.Key.Type,
                Client = group.Key.Client,
                TotalIncome = group.Sum(payment => payment.Amount),
                TotalProfit = GetTotalProfit(group),
                TotalExpenses = GetTotalExpenses(group),
                TotalVat = group.Sum(payment => payment.VatAmount)
            });

        var uniqueCurrencies = queriedPayments.Select(payment => payment.Currency).Distinct();

        // Prepare data for each CurrencyIncomeDetails
        var dataForCharts = new Dictionary<string, Dictionary<string, decimal>>();
        foreach (var currency in uniqueCurrencies)
        {
            var totalCurrencyData = groupedData.Where(item => item.Currency == currency);
            var totalIncome = totalCurrencyData.Sum(x => x.TotalIncome);
            var totalVat = totalCurrencyData.Sum(x => x.TotalVat);
            var totalProfit = totalCurrencyData.Sum(x => x.TotalProfit);
            var totalExpenses = totalCurrencyData.Sum(x => x.TotalExpenses);

            // Prepare data for charts
            var typeData = groupedData
                .Where(item => item.Currency == currency)
                .GroupBy(item => item.Type)
                .ToDictionary(
                    group => group.Key,
                    group => group.Sum(item => item.TotalProfit));

            var clientData = groupedData
                .Where(item => item.Currency == currency)
                .GroupBy(item => item.Client)
                .ToDictionary(
                    group => group.Key,
                    group => group.Sum(item => item.TotalProfit));

            dataForCharts.Add($"{currency} Profit Per Project Type", typeData);
            dataForCharts.Add($"{currency} Profit Per Client", clientData);
            var charts = CreateCharts(dataForCharts);
            dataForCharts.Clear();

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
    private List<PaymentViewModel> GetQueriedPayments()
    {
        var currencies = QueryCurrencies.OfType<string>();
        var types = QueryTypes.OfType<string>();

        return PaymentManager.Query(currencies, types, QueryStartDate, QueryEndDate)
                                                                   .Select(x => new PaymentViewModel(x))
                                                                   .ToList();
    }
    private decimal GetTotalProfit(IGrouping<object, PaymentViewModel> group)
    {
        // Collection of associated projects whose TotalExpenses are already accounted for
        List<Project> assProjects = new();
        var totalProfit = 0m;

        foreach (var payment in group)
        {
            var assProject = payment.Project;
            // Account for VAT portion of payment
            totalProfit += payment.Amount / (1 + assProject.VatRateDecimal);

            if (!assProjects.Contains(assProject))
            {
                totalProfit -= assProject.Expenses.Sum(e => e.Amount);
                assProjects.Add(assProject);
            }
        }

        return totalProfit;
    }
    private decimal GetTotalExpenses(IGrouping<object, PaymentViewModel> group)
    {
        // Collection of associated projects whose TotalExpenses are already accounted for
        List<Project> assProjects = new();
        var totalExpenses = 0m;

        foreach (var payment in group)
        {
            var assProject = payment.Project;
            if (!assProjects.Contains(assProject))
            {
                totalExpenses += assProject.Expenses.Sum(e => e.Amount);
                assProjects.Add(assProject);
            }
        }
        return totalExpenses;
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
    public void CreateCurrencyExpectedEarningsViewModels()
    {
        CurrencyExpectedIncome.Clear();

        var projects = ProjectManager.AllProjects.Select(x => new ProjectViewModel(x));

        var activeAndInvoicedProjects = projects.Where(x => x.Status == Project.ProjectStatus.Active || x.Status == Project.ProjectStatus.Invoiced);
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
            QueryStartDate = ProjectManager.AllProjects.Min(x => x.Date);
            QueryEndDate = DateTime.Today;
        }
    }
    public class ChartWithHeader
    {
        public Chart Chart { get; set; }
        public string Header { get; set; }
    }
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
