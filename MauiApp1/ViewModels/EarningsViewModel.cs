using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MauiApp1.Models;
using Microcharts;
using Microcharts.Maui;
using SkiaSharp;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;

namespace MauiApp1.ViewModels;

public partial class EarningsViewModel : ObservableObject
{
    [ObservableProperty]
    DateTime queryStartDate = ProjectManager.AllProjects.Min(x => x.Date);

    [ObservableProperty]
    DateTime queryEndDate = DateTime.Today;

    [ObservableProperty]
    ObservableCollection<object> queryCurrencies;

    [ObservableProperty]
    ObservableCollection<string> currencyList;

    [ObservableProperty]
    ObservableCollection<object> queryTypes;

    [ObservableProperty]
    ObservableCollection<string> typeList;

    [ObservableProperty]
    ObservableCollection<CurrencyExpectedPaymentsViewModel> currencyExpectedIncome;

    [ObservableProperty]
    ObservableCollection<CurrencyIncomeDetails> currencyIncomeDetailsCollection;

    [ObservableProperty]
    ObservableCollection<ChartWithHeader> charts;
    public CultureInfo CurrentCulture => CultureInfo.CurrentCulture;


    public EarningsViewModel()
    {
        CurrencyList = new (Settings.Currencies);
        QueryCurrencies = new();
        TypeList = new(Settings.ProjectTypes);
        QueryTypes = new();
        CurrencyIncomeDetailsCollection = new();
        CurrencyExpectedIncome = new();
        Charts = new();
    }

    //Called inside IncomePage.OnAppearing to make sure changes in projects are accounted for
    [RelayCommand]
    public void ApplyFilters()
    {
        var queriedPayments = GetQueriedPayments();

        // Group by Currency, Type, and Client
        var groupedData = queriedPayments
            .GroupBy(payment => new { payment.Currency, payment.Type, payment.Client })
            .Select(group => new
            {
                Currency = group.Key.Currency,
                Type = group.Key.Type,
                Client = group.Key.Client,
                TotalAmount = group.Sum(payment => payment.Amount),
                TotalProfit = GetTotalProfit(group),
                TotalVat = group.Sum(payment => payment.VatAmount)
            });

        var uniqueCurrencies = queriedPayments.Select(payment => payment.Currency).Distinct();

        // Prepare data for each graph (total payments per type and per client for each currency)
        var dataForCharts = new Dictionary<string, Dictionary<string, decimal>>();
        foreach (var currency in uniqueCurrencies)
        {
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

            dataForCharts.Add($"{currency} Earnings Per Project Type", typeData);
            dataForCharts.Add($"{currency} Earnings Per Client", clientData);
        }

        CreateCharts(dataForCharts);
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
        // If expenses of project are greater than payments so far, return 0
        return totalProfit > 0 ? totalProfit : 0;
    }
    private void CreateCharts(Dictionary<string, Dictionary<string, decimal>> dataForCharts)
    {
        Charts.Clear();

        foreach (var chartData in dataForCharts)
        {
            var chartEntries = new List<ChartEntry>();
            chartEntries.AddRange(ConvertToChartEntries(chartData.Value));
            Charts.Add(new ChartWithHeader
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
    }
    private static List<ChartEntry> ConvertToChartEntries(Dictionary<string, decimal> data)
    {
        var chartEntries = new List<ChartEntry>();
        Random rnd = new Random();
        foreach (var kvp in data)
        {
            chartEntries.Add(new ChartEntry((float)kvp.Value)
            {
                Label = kvp.Key,
                ValueLabel = kvp.Value.ToString("N0"),
                Color = SkiaSharp.SKColor.Parse(String.Format("#{0:X6}", rnd.Next(0x1000000)))
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
            var expectedPaymentsAmount = activeAndInvoicedProjects.Where(x => x.Currency == currency).Sum(x => x.TotalExpectedPaymentsAmount - x.PaidAmount);
            CurrencyExpectedIncome.Add(new CurrencyExpectedPaymentsViewModel
            {
                Currency = currency,
                Amount = expectedPaymentsAmount >= 0 ? expectedPaymentsAmount : 0
            });
        }
    }
    public class ChartWithHeader
    {
        public Chart Chart { get; set; }
        public string Header { get; set; }
    }
    public class CurrencyIncomeDetails
    {
        string Currency { get; set; }
        decimal IncomeAmount { get; set; }
        decimal ProfitAmount { get; set; }
        decimal VatAmount { get; set; }
        List<ChartWithHeader> Charts { get; set; }
    }

    // Class denoting total expected currency payments [Sum of (Fee - Sum(Payments)) from active and invoiced projects]
    public class CurrencyExpectedPaymentsViewModel
    {
        public string Currency { get; set; }
        public decimal Amount { get; set; }
    }
}
