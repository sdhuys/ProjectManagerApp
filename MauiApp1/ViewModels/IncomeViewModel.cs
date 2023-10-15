using CommunityToolkit.Mvvm.ComponentModel;
using MauiApp1.Models;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace MauiApp1.ViewModels
{
    public partial class IncomeViewModel : ObservableObject
    {
        [ObservableProperty]
        DateTime queryStartDate = DateTime.MinValue;

        [ObservableProperty]
        DateTime queryEndDate = DateTime.Today;

        [ObservableProperty]
        List<string> queryCurrencies = new List<string> { "EUR", "USD" };

        [ObservableProperty]
        List<string> queryTypes = new List<string> { "Mural", "Illustration" };

        [ObservableProperty]
        List<PaymentsGroupedByCurrency> filteredPaymentsGroupedByCurrency = new();

        [ObservableProperty]
        List<PaymentsGroupedByCurrencyAndType> filteredPaymentsGroupedByCurrencyAndType = new();

        [ObservableProperty]
        List<PaymentsGroupedByCurrencyAndClient> filteredPaymentsGroupedByCurrencyAndClient = new();
        
        public decimal TotalQueriedIncome => FilteredPaymentsGroupedByCurrency.Select(x => x.TotalAmount).Sum();

        //Called inside IncomePage.OnAppearing
        public void FilterAndSetGroupings()
        {
            var paymentViewModels = PaymentManager.Query(QueryCurrencies, QueryTypes, QueryStartDate, QueryEndDate)
                                                                       .Select(x => new PaymentViewModel(x));

            FilteredPaymentsGroupedByCurrency = paymentViewModels.GroupBy(x => x.Currency)
                                                                 .Select(x => new PaymentsGroupedByCurrency(x.Key, x.ToList()))
                                                                 .ToList();

            FilteredPaymentsGroupedByCurrencyAndType = paymentViewModels.GroupBy(x => new { x.Currency, x.Project.Type })
                                                                        .Select(x => new PaymentsGroupedByCurrencyAndType(x.Key.Currency, x.Key.Type, x.ToList()))
                                                                        .ToList();

            FilteredPaymentsGroupedByCurrencyAndClient = paymentViewModels.GroupBy(x => new { x.Currency, x.Project.Client })
                                                                          .Select(x => new PaymentsGroupedByCurrencyAndClient(x.Key.Currency, x.Key.Client, x.ToList()))
                                                                          .ToList();
        }
    }

    public class PaymentsGroupedByCurrency : List<PaymentViewModel>
    {
        public string Currency { get; }
        public decimal TotalAmount { get; }

        public PaymentsGroupedByCurrency(string currency,List<PaymentViewModel> payments)
        {
            Currency = currency;
            AddRange(payments);
            TotalAmount = payments.Sum(p => p.Amount);
        }
    }

    public class PaymentsGroupedByCurrencyAndType : List<PaymentViewModel>
    {
        public string Currency { get; }
        public string ProjectType { get; }
        public decimal TotalAmount { get; }

        public PaymentsGroupedByCurrencyAndType(string currency, string projectType, List<PaymentViewModel> payments)
        {
            Currency = currency;
            ProjectType = projectType;
            AddRange(payments);
            TotalAmount = payments.Sum(p => p.Amount);
        }
    }

    public class PaymentsGroupedByCurrencyAndClient : List<PaymentViewModel>
    {
        public string Currency { get; }
        public string Client { get; }
        public decimal TotalAmount { get; }

        public PaymentsGroupedByCurrencyAndClient(string currency, string client, List<PaymentViewModel> payments)
        {
            Currency = currency;
            Client = client;
            AddRange(payments);
            TotalAmount = payments.Sum(p => p.Amount);
        }
    }
}
