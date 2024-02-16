namespace MauiApp1.Models;

public static class PaymentManager
{
    public static List<Payment> AllPayments { get; } = new List<Payment>();
    public static void AddPayment(Payment payment)
    {
        AllPayments.Add(payment);
    }
    public static void RemovePayment(Payment payment)
    {
        AllPayments.Remove(payment);
    }

    public static IEnumerable<Payment> QueryByCurrenciesTypesAgentsAndDate(IEnumerable<string> currencies, IEnumerable<string> types, IEnumerable<Agent> agents, DateTime startDate, DateTime endDate)
    {
        var projects = ProjectManager.AllProjects;

        var result = AllPayments.Where(x =>
        {
            var project = projects.FirstOrDefault(p => x.AssociatedProjectID == p.Id);
            return project != null &&
                   currencies.Contains(project.Currency) &&
                   types.Contains(project.Type) &&
                   (agents.Contains(project.Agent) || agents.FirstOrDefault(a => a?.Name == project.Agent?.Name) != null) && // check for agent with same name (if multiple with diff % are saved in settings)
                   x.Date >= startDate &&
                   x.Date <= endDate;
        });
        return result;
    }

    public static IEnumerable<Payment> QueryByCurrencyAndMonth(string currency, DateTime date)
    {
        var projects = ProjectManager.AllProjects;

        var result = AllPayments.Where(x =>
        {
            var project = projects.FirstOrDefault(p => x.AssociatedProjectID == p.Id);
            return project != null &&
                   project.Currency == currency &&
                   x.Date.Month == date.Month &&
                   x.Date.Year == date.Year;
        });
        return result;
    }
}
