namespace MauiApp1.Models;

public static class PaymentQueryManager
{
    public static IEnumerable<Payment> QueryByCurrenciesTypesAgentsAndDate(IEnumerable<string> currencies, IEnumerable<string> types, IEnumerable<Agent> agents, DateTime startDate, DateTime endDate)
    {
        var queriedProjectsByCurrencyTypeAgent = ProjectManager.AllProjects.Where(p => currencies.Contains(p.Currency) &&
                                                           types.Contains(p.Type) &&
                                                           (agents.Contains(p.Agent) ||
                                                           agents.FirstOrDefault(a => a?.Name == p.Agent?.Name) != null)); // check for agent with same name (if multiple with diff % are saved in settings)

        var paymentsWithinQueryDates = queriedProjectsByCurrencyTypeAgent.SelectMany(p => p.Payments).Where(x => x.Date >= startDate && x.Date <= endDate);
        return paymentsWithinQueryDates;
    }

    public static IEnumerable<Payment> QueryByCurrencyAndMonth(string currency, DateTime date)
    {
        var queriedProjectsByCurrency = ProjectManager.AllProjects.Where(p => p.Currency == currency);
        var paymentsWithinMonth = queriedProjectsByCurrency.SelectMany(p => p.Payments).Where(x => x.Date.Month == date.Month && x.Date.Year == date.Year);

        return paymentsWithinMonth;
    }
}
