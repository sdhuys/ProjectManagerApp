﻿using CommunityToolkit.Mvvm.ComponentModel;
using MauiApp1.Models;
using System.Diagnostics;

namespace MauiApp1.ViewModels;

public partial class PaymentViewModel : ObservableObject
{
    public Payment Payment { get; }
    public Project Project { get; }
    public decimal Amount => Payment.Amount;
    public decimal VatAmount => Amount - (Amount / (1 + Project.VatRateDecimal));
    public DateTime Date => Payment.Date;
    public string Type => Project.Type;
    public string Client => Project.Client;
    public string Currency => Project.Currency;


    public PaymentViewModel(Payment payment)
    {
        Project = ProjectManager.AllProjects.FirstOrDefault(p => p.Id == payment.AssociatedProjectID);
        Payment = payment;
        Debug.WriteLine($"Amount = {Amount}, VAT amount = {VatAmount}");
    }
}
