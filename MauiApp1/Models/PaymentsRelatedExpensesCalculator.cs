using MauiApp1.ViewModels;

namespace MauiApp1.Models;

// Calculates the total project expenses of a collection of PaymentViewModels
// When filtering payments on date, only count expenses made within that timeframe
// For relative/profit sharing expenses, only include the expense if the project is Finished or Cancelled
// ^ in case the expense is added before the project is concluded, but likely only paid out afterwards
public static class PaymentsRelatedExpensesCalculator
{
    public static decimal CalculateRelatedExpenses(IEnumerable<PaymentViewModel> payments)
    {
        return CalculateRelatedExpensesInternal(payments, null, null);
    }

    public static decimal CalculateRelatedExpenses(IEnumerable<PaymentViewModel> payments, DateTime startDate, DateTime endDate)
    {
        return CalculateRelatedExpensesInternal(payments, startDate, endDate);
    }

    public static decimal CalculateRelatedExpenses(IEnumerable<PaymentViewModel> payments, DateTime queriedMonth)
    {
        return CalculateRelatedExpensesInternal(payments, new DateTime(queriedMonth.Year, queriedMonth.Month, 1), new DateTime(queriedMonth.Year, queriedMonth.Month, DateTime.DaysInMonth(queriedMonth.Year, queriedMonth.Month)));
    }

    private static decimal CalculateRelatedExpensesInternal(IEnumerable<PaymentViewModel> payments, DateTime? startDate, DateTime? endDate)
    {
        List<Project> assProjects = new();
        var totalExpenses = 0m;

        foreach (var payment in payments)
        {
            var assProject = payment.Project;
            if (!assProjects.Contains(assProject))
            {
                var absoluteExpenses = assProject.Expenses.Where(e => !e.IsRelative);
                var relevantAbsoluteExpenses = FilterExpensesByDate(absoluteExpenses, startDate, endDate);

                var relExpenses = assProject.Expenses.Where(e => e.IsRelative);
                var relevantRelExpenses = FilterExpensesByDate(relExpenses, startDate, endDate);

                totalExpenses += relevantAbsoluteExpenses.Sum(e => e.Amount);

                if (assProject.Status == Project.ProjectStatus.Completed || assProject.Status == Project.ProjectStatus.Cancelled)
                {
                    totalExpenses += relevantRelExpenses.Sum(e => e.Amount);
                }

                assProjects.Add(assProject);
            }
        }
        return totalExpenses;
    }

    private static IEnumerable<ProjectExpense> FilterExpensesByDate(IEnumerable<ProjectExpense> expenses, DateTime? startDate, DateTime? endDate)
    {
        if (startDate.HasValue && endDate.HasValue)
        {
            return expenses.Where(e => e.Date >= startDate && e.Date <= endDate);
        }
        return expenses;
    }
}