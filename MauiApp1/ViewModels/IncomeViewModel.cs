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

public partial class IncomeViewModel : ObservableObject
{
    [ObservableProperty]
    DateTime queryStartDate = DateTime.MinValue;

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
    Chart chart;

    [ObservableProperty]
    ObservableCollection<ChartWithHeader> charts = new();
    public CultureInfo CurrentCulture => CultureInfo.CurrentCulture;

    public IncomeViewModel()
    {
        CurrencyList = new (Settings.Currencies);
        QueryCurrencies = new();
        TypeList = new(Settings.ProjectTypes);
        QueryTypes = new();
    }

    //Called inside IncomePage.OnAppearing to make sure changes in projects are accounted for
    [RelayCommand]
    public void ApplyFilters()
    {
        var currencies = QueryCurrencies.OfType<string>();
        var types = QueryTypes.OfType<string>();

        var queriedPayments = PaymentManager.Query(currencies, types, QueryStartDate, QueryEndDate)
                                                                   .Select(x => new PaymentViewModel(x))
                                                                   .ToList();
        Debug.WriteLine($"Payments after filter: {queriedPayments.Count}");

        var uniqueCurrencies = queriedPayments.Select(payment => payment.Currency).Distinct();

        // Group by Currency, Type, and Client
        var groupedData = queriedPayments
            .GroupBy(payment => new { payment.Currency, payment.Type, payment.Client })
            .Select(group => new
            {
                Currency = group.Key.Currency,
                Type = group.Key.Type,
                Client = group.Key.Client,
                TotalAmount = group.Sum(payment => payment.Amount)
            });

        // Prepare data for each graph (total payments per type and per client for each currency)
        var dataForCharts = new Dictionary<string, Dictionary<string, decimal>>();
        foreach (var currency in uniqueCurrencies)
        {
            var typeData = groupedData
                .Where(item => item.Currency == currency)
                .GroupBy(item => item.Type)
                .ToDictionary(
                    group => group.Key,
                    group => group.Sum(item => item.TotalAmount));

            var clientData = groupedData
                .Where(item => item.Currency == currency)
                .GroupBy(item => item.Client)
                .ToDictionary(
                    group => group.Key,
                    group => group.Sum(item => item.TotalAmount));

            dataForCharts.Add($"{currency} Earnings Per Project Type", typeData);
            dataForCharts.Add($"{currency} Earnings Per Client", clientData);
        }

        CreateCharts(dataForCharts);
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
                Name = chartData.Key
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
    public class ChartWithHeader
    {
        public Chart Chart { get; set; }
        public string Name { get; set; }
    }
}
