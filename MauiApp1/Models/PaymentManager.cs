﻿namespace MauiApp1.Models;

public static class PaymentManager
{
    public static List<Payment> AllPayments { get; set; } = new List<Payment>();
    public static void AddPayment(Payment payment)
    {
        AllPayments.Add(payment);
    }
    public static void RemovePayment(Payment payment) => AllPayments.Remove(payment);
    public static IEnumerable<Payment> Query(IEnumerable<string> currencies, IEnumerable<string> types, IEnumerable<Agent> agents, DateTime startDate, DateTime endDate)
    {
        var projects = ProjectManager.AllProjects;

        var result = AllPayments.Where(x =>
        {
            var project = projects.FirstOrDefault(p => x.AssociatedProjectID == p.Id);
            return project != null &&
                   currencies.Contains(project.Currency) &&
                   types.Contains(project.Type) &&
                   agents.Contains(project.Agent) &&
                   x.Date >= startDate &&
                   x.Date <= endDate;
        });
        return result;
    }
}
