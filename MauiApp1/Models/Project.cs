using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using CommunityToolkit.Mvvm;

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
    public decimal TotalExpenses
    {
        get
        {
            return Expenses == null ? 0 : Expenses.Sum(x => x.Amount);
        }
    }
    public Agent Agent { get; set; }
    public decimal AgencyFeeDecimal { get; set; }
    public decimal Profit
    {
        get
        {
            return Fee - TotalExpenses - (AgencyFeeDecimal * Fee);
        }
    }
    public bool IsPaid { get; set; }


    public Project(string client, string type, string description, string currency, decimal fee, Agent agent, decimal agencyFeeDecimal, List<Expense> expenses, bool isPaid)
    {
        Id = Guid.NewGuid();
        Client = client;
        Type = type;
        Description = description;
        Date = DateTime.Now;
        Currency = currency;
        Fee = fee;
        Agent = agent;
        Expenses = expenses;
        AgencyFeeDecimal = agencyFeeDecimal;
        IsPaid = isPaid;
    }
}
