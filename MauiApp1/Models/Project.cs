﻿using MauiApp1.StaticHelpers;
using Newtonsoft.Json;

namespace MauiApp1.Models;

public class Project
{
    public string Id { get; private set; }
    public string Client { get; set; }
    public string Type { get; set; }
    public string Description { get; set; }
    public DateTime Date { get; set; }
    public string Currency { get; set; }
    public decimal Fee { get; set; }
    public bool IsVatIncluded { get; set; }
    public decimal VatRateDecimal { get; set; }
    public List<ProjectExpense> Expenses { get; set; }
    public Agent Agent { get; set; }
    public decimal AgencyFeeDecimal { get; set; }
    public List<Payment> Payments { get; set; }
    public ProjectStatus Status { get; set; }

    public Project(string client, string type, string description, DateTime date, string currency, decimal fee, bool vatIncluded, decimal vat_RateDecimal, Agent agent, decimal agencyFeeDecimal, List<ProjectExpense> expenses, List<Payment> payments, ProjectStatus status)
    {
        Id = Guid.NewGuid().ToString();
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
        }
        Status = status;
    }

    [JsonConstructor]
    public Project(string id, string client, string type, string description, DateTime date, string currency, decimal fee, bool vatIncluded, decimal vat_RateDecimal, Agent agent, decimal agencyFeeDecimal, List<ProjectExpense> expenses, List<Payment> payments, ProjectStatus status)
    {
        Id = id;
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
        }
        Status = status;
    }

    public enum ProjectStatus
    {
        Active,
        Invoiced,
        Completed,
        Cancelled
    }
}
