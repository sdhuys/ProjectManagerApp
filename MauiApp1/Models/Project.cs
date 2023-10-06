namespace MauiApp1.Models;

public class Project
{
    public Guid Id { get; set; }
    public string Client { get; set; }
    public string Type { get; set; }
    public string Description { get; set; }
    public DateTime Date { get; set; }
    public string Currency { get; set; }
    public decimal Fee { get; set; }
    public List<Expense> Expenses { get; set; }
    public decimal TotalExpenses => Expenses == null ? 0 : Expenses.Sum(x => x.Amount);
    public Agent Agent { get; set; }
    public decimal AgencyFeeDecimal { get; set; }
    public decimal Profit => Fee - TotalExpenses - (AgencyFeeDecimal * Fee);
    public List<Payment> Payments { get; set; }
    public decimal PaidAmount => Payments.Sum(x => x.Amount);
    public decimal PaidPercentage => (PaidAmount / (Fee - (AgencyFeeDecimal * Fee))) * 100m;
    public ProjectStatus Status { get; set; }


    public Project(string client, string type, string description, DateTime date, string currency, decimal fee, Agent agent, decimal agencyFeeDecimal, List<Expense> expenses, List<Payment> payments, ProjectStatus status)
    {
        Id = Guid.NewGuid();
        Client = client;
        Type = type;
        Description = description;
        Date = date;
        Currency = currency;
        Fee = fee;
        Agent = agent;
        Expenses = expenses;
        AgencyFeeDecimal = agencyFeeDecimal;
        Payments = payments;
        Status = status;
    }

    public enum ProjectStatus
    {
        Active,
        Invoiced,
        Finished,
        Cancelled
    }
}
