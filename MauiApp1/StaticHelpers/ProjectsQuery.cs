using MauiApp1.ViewModels;
using MauiApp1.Models;

namespace MauiApp1.StaticHelpers;

static class ProjectsQuery
{
    public static IEnumerable<Project> ByCurrencyTypeAndAgent(IEnumerable<string> currencies, IEnumerable<string> types, IEnumerable<Agent> agents)
    {
        return ProjectManager.AllProjects.Where(p => currencies.Contains(p.Currency) && types.Contains(p.Type) && (agents.Contains(p.Agent) || agents.FirstOrDefault(a => a?.Name == p.Agent?.Name) != null));
    }

    public static IEnumerable<PaymentViewModel> GetProjectPaymentsWithinDates(IEnumerable<Project> projects, DateTime startDate, DateTime endDate)
    {
        return projects.SelectMany(p => p.Payments).Where(x => x.Date >= startDate && x.Date <= endDate).Select(x => new PaymentViewModel(x));
    }

    public static IEnumerable<ProjectExpense> GetProjectExpensesWithinDates(IEnumerable<Project> projects, DateTime startDate, DateTime endDate)
    {
        var absoluteExpenses = projects.SelectMany(p => p.Expenses).Where(x => !x.IsRelative && x.Date >= startDate && x.Date <= endDate);
        var finishedProjects = GetFinishedProjects(projects);
        var paidRelativeExpenses = finishedProjects.SelectMany(p => p.Expenses).Where(x => x.IsRelative && x.Date >= startDate && x.Date <= endDate);

        return absoluteExpenses.Union(paidRelativeExpenses);
    }

    public static IEnumerable<Project> GetFinishedProjects(IEnumerable<Project> projects)
    {
        return projects.Where(p => p.Status == Project.ProjectStatus.Completed || p.Status == Project.ProjectStatus.Cancelled);
    }

    public static decimal CalculateMonthProfitForCurrency(DateTime date, string currency)
    {
        var currencyProjects = ProjectManager.AllProjects.Where(p => p.Currency == currency);
        var finishedProjects = GetFinishedProjects(currencyProjects);

        var paymentVMs = currencyProjects.SelectMany(p => p.Payments).Where(x => x.Date.Month == date.Month && x.Date.Year == date.Year).Select(p => new PaymentViewModel(p));

        var absoluteExpenses = currencyProjects.SelectMany(p => p.Expenses).Where(x => !x.IsRelative && x.Date.Month == date.Month && x.Date.Year == date.Year).Sum(e => e.Amount);
        var relativeExpenses = finishedProjects.SelectMany(p => p.Expenses).Where(x => x.IsRelative && x.Date.Month == date.Month && x.Date.Year == date.Year).Sum(e => e.Amount);

        var totalPaymentsAmount = paymentVMs.Sum(p => p.Amount);
        var totalPaymentsVatAmount = paymentVMs.Sum(p => p.VatAmount);

        var totalExpenses = absoluteExpenses + relativeExpenses;


        return totalPaymentsAmount - (totalExpenses + totalPaymentsVatAmount);
    }
}
