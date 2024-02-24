using MauiApp1.ViewModels;

namespace MauiApp1.StaticHelpers;

static class ProfitCalculator
{
    public static decimal CalculateMonthProfitForCurrency(DateTime date, string currency)
    {
        var currencyProjects = ProjectManager.AllProjects.Where(p => p.Currency == currency);
        var finishedProjects = currencyProjects.Where(p => (int)p.Status > 1);

        var paymentVMs = currencyProjects.SelectMany(p => p.Payments).Where(x => x.Date.Month == date.Month && x.Date.Year == date.Year).Select(p => new PaymentViewModel(p));

        var absoluteExpenses = currencyProjects.SelectMany(p => p.Expenses).Where(x => !x.IsRelative && x.Date.Month == date.Month && x.Date.Year == date.Year).Sum(e => e.Amount);
        var relativeExpenses = finishedProjects.SelectMany(p => p.Expenses).Where(x => x.IsRelative && x.Date.Month == date.Month && x.Date.Year == date.Year).Sum(e => e.Amount);

        var totalPaymentsAmount = paymentVMs.Sum(p => p.Amount);
        var totalPaymentsVatAmount = paymentVMs.Sum(p => p.VatAmount);

        var totalExpenses = absoluteExpenses + relativeExpenses;


        return totalPaymentsAmount - (totalExpenses + totalPaymentsVatAmount);
    }
}
