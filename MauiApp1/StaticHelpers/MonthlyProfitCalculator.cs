using MauiApp1.ViewModels;

namespace MauiApp1.StaticHelpers;

static class MonthlyProfitCalculator
{
    public static decimal CalculateMonthProfitForCurrency(DateTime date, string currency)
    {
        var payments = PaymentQueryManager.QueryByCurrencyAndMonth(currency, date).Select(x => new PaymentViewModel(x));
        var totalPaymentsAmount = payments.Sum(p => p.Amount);
        var totalPaymentsVatAmount = payments.Sum(p => p.VatAmount);
        var totalRelatedExpenses = PaymentsRelatedExpensesCalculator.CalculateRelatedExpenses(payments, date);

        return totalPaymentsAmount - (totalRelatedExpenses + totalPaymentsVatAmount);
    }
}
