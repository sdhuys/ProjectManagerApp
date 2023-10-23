using System.Diagnostics;

namespace MauiApp1.Models;

public static class PaymentManager
{
    public static List<Payment> AllPayments { get; set; } = new List<Payment>();
    public static void AddPayment(Payment payment)
    {
        AllPayments.Add(payment);
        Debug.WriteLine("PAYMENT ADDED");
    }
    public static void RemovePayment(Payment payment) => AllPayments.Remove(payment);
    public static IEnumerable<Payment> Query(IEnumerable<string> currencies, IEnumerable<string> types, DateTime startDate, DateTime endDate)
    {
        Debug.WriteLine("QUERY START");
        Debug.WriteLine($"Payment Count: {AllPayments.Count}");
        var projects = ProjectManager.AllProjects;
        Debug.WriteLine($"Projects Total: {projects.Count}");

        var result = AllPayments.Where(x =>
        {
            var project = projects.FirstOrDefault(p => x.AssociatedProjectID == p.Id);
            return project != null &&
                   currencies.Contains(project.Currency) &&
                   types.Contains(project.Type) &&
                   x.Date >= startDate &&
                   x.Date <= endDate;
        });
        return result;
    }
}
