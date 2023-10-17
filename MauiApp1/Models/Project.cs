using System.Diagnostics;

namespace MauiApp1.Models;

public class Project
{
    public Guid Id { get; }
    public string Client { get; set; }
    public string Type { get; set; }
    public string Description { get; set; }
    public DateTime Date { get; set; }
    public string Currency { get; set; }
    public decimal Fee { get; set; }
    public bool IsVatIncluded { get; set; }
    public decimal VatRateDecimal { get; set; }
    public List<Expense> Expenses { get; set; }
    public Agent Agent { get; set; }
    public decimal AgencyFeeDecimal { get; set; }
    public List<Payment> Payments { get; set; }
    public ProjectStatus Status { get; set; }


    public Project(string client, string type, string description, DateTime date, string currency, decimal fee, bool vatIncluded, decimal vat_RateDecimal, Agent agent, decimal agencyFeeDecimal, List<Expense> expenses, List<Payment> payments, ProjectStatus status)
    {
        Id = Guid.NewGuid();
        Client = client;
        Type = type;
        Description = description;
        Date = date;
        Currency = currency;
        Fee = fee;
        IsVatIncluded = vatIncluded;
        VatRateDecimal = vat_RateDecimal;
        Agent = agent;
        Expenses = expenses;
        AgencyFeeDecimal = agencyFeeDecimal;
        Payments = payments;
        foreach (var payment in Payments)
        {
            payment.AssociatedProjectID = Id;
            Debug.WriteLine($"ID: {Id}");
            Debug.WriteLine($"ASSID: {payment.AssociatedProjectID}");

        }
        Status = status;
        ProjectManager.AllProjects.Add(this);
    }
    public enum ProjectStatus
    {
        Active,
        Invoiced,
        Finished,
        Cancelled
    }
}
