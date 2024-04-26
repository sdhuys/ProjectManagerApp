using MauiApp1.StaticHelpers;

namespace MauiApp1.Models;

public partial class PaymentWrapper
{
    public Payment Payment { get; }
    public Project Project { get; }
    public decimal Amount => Payment.Amount;
    public decimal VatAmount => Amount - (Amount / (1 + Project.VatRateDecimal));
    public DateTime Date => Payment.Date;
    public string Type => Project.Type;
    public string Client => Project.Client;
    public string Currency => Project.Currency;
    public Agent Agent => Project.Agent;

    public PaymentWrapper(Payment payment, IEnumerable<Project> projects)
    {
        Project = projects.FirstOrDefault(p => p.Id == payment.AssociatedProjectID);
        Payment = payment;
    }
}
