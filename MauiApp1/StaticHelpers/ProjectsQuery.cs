using MauiApp1.ViewModels;
using MauiApp1.Models;

namespace MauiApp1.StaticHelpers;

static class ProjectsQuery
{
    public static IEnumerable<Project> ByCurrencyTypeAndAgent(IEnumerable<string> currencies, IEnumerable<string> types, IEnumerable<Agent> agents, IEnumerable<Project> projects)
    {
        return projects.Where(p => currencies.Contains(p.Currency) && types.Contains(p.Type) 
                                                && (agents.Contains(p.Agent) 
                                                    || agents.FirstOrDefault(a => a?.Name == p.Agent?.Name) != null)); // check for agent with same name (if multiple with diff % are saved in settings)

    }

    public static IEnumerable<PaymentWrapper> GetProjectPaymentsWithinDates(IEnumerable<Project> projects, DateTime startDate, DateTime endDate)
    {
        return projects.SelectMany(p => p.Payments).Where(x => x.Date >= startDate && x.Date <= endDate).Select(x => new PaymentWrapper(x, projects));
    }

    public static IEnumerable<ProjectExpense> GetProjectExpensesWithinDates(IEnumerable<Project> projects, DateTime startDate, DateTime endDate)
    {
        var absoluteExpenses = projects.SelectMany(p => p.Expenses).Where(x => x is not ProfitSharingExpense && x.Date >= startDate && x.Date <= endDate);
        var paidRelativeExpenses = projects.SelectMany(p => p.Expenses).OfType<ProfitSharingExpense>().Where(x => x.IsPaid && x.Date >= startDate && x.Date <= endDate);

        return absoluteExpenses.Union(paidRelativeExpenses);
    }

    public static decimal CalculateMonthProfitForCurrency(DateTime date, string currency, IEnumerable<Project> projects)
    {
        var currencyProjects = projects.Where(p => p.Currency == currency);

        var paymentVMs = currencyProjects.SelectMany(p => p.Payments).Where(x => x.Date.Month == date.Month && x.Date.Year == date.Year).Select(p => new PaymentWrapper(p, currencyProjects));
        var totalPaymentsAmount = paymentVMs.Sum(p => p.Amount);
        var totalPaymentsVatAmount = paymentVMs.Sum(p => p.VatAmount);

        var absoluteExpensesAmount = currencyProjects.SelectMany(p => p.Expenses).Where(x => x is not ProfitSharingExpense && x.Date.Month == date.Month && x.Date.Year == date.Year).Sum(e => e.Amount);
        var paidRelativeExpensesAmount = currencyProjects.SelectMany(p => p.Expenses).OfType<ProfitSharingExpense>().Where(x => x.IsPaid && x.Date.Month == date.Month && x.Date.Year == date.Year).Sum(e => e.Amount);
        var totalExpenses = absoluteExpensesAmount + paidRelativeExpensesAmount;


        return totalPaymentsAmount - (totalExpenses + totalPaymentsVatAmount);
    }
}
