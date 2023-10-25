﻿using CommunityToolkit.Mvvm.ComponentModel;
using MauiApp1.Models;

namespace MauiApp1.ViewModels;

//Class that wraps Project class to represent projects dynamically on ProjectsPage
public class ProjectViewModel : ObservableObject
{
    public Project Project { get; }
    public Guid Id => Project.Id;
    public string Client
    {
        get => Project.Client;
        set
        {
            Project.Client = value;
            OnPropertyChanged();
        }
    }
    public string Type
    {
        get => Project.Type;
        set
        {
            Project.Type = value;
            OnPropertyChanged();
        }
    }
    public string Description
    {
        get => Project.Description;
        set
        {
            Project.Description = value;
            OnPropertyChanged();
        }
    }
    public DateTime Date
    {
        get => Project.Date;
        set
        {
            Project.Date = value;
            OnPropertyChanged();
        }
    }
    public string Currency
    {
        get => Project.Currency;
        set
        {
            Project.Currency = value;
            OnPropertyChanged();
        }
    }
    public decimal Fee
    {
        get => Project.Fee;
        set
        {
            Project.Fee = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(Profit));
            OnPropertyChanged(nameof(PaidPercentage));
        }
    }
    public decimal FeeExcludingVat => IsVatIncluded ? Fee / (1 + VatRateDecimal) : Fee;
    public decimal FeeIncludingVat => IsVatIncluded ? Fee : Fee + (Fee * VatRateDecimal);
    public bool IsVatIncluded
    {
        get => Project.IsVatIncluded;
        set
        {
            Project.IsVatIncluded = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(VatStatus));
        }
    }
    public decimal VatRateDecimal
    {
        get => Project.VatRateDecimal;
        set
        {
            Project.VatRateDecimal = value;
            OnPropertyChanged();
        }
    }
    public decimal VatAmount => IsVatIncluded ? Fee - (Fee / (1 + VatRateDecimal)) : Fee * VatRateDecimal;
    public string VatStatus
    {
        get
        {
            if (VatRateDecimal == 0m) return null;
            return IsVatIncluded ? "Incl." : "Excl.";
        }
    }
    public List<Expense> Expenses
    {
        get => Project.Expenses;
        set
        {
            Project.Expenses = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(TotalExpenses));
            OnPropertyChanged(nameof(Profit));
        }
    }
    public decimal TotalExpenses => Expenses == null ? 0 : Expenses.Sum(x => x.Amount);
    public Agent Agent
    {
        get => Project.Agent;
        set
        {
            Project.Agent = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ManagedByAgent));
        }
    }
    public bool ManagedByAgent => Agent != null;
    public decimal AgencyFeeDecimal
    {
        get => Project.AgencyFeeDecimal;
        set
        {
            Project.AgencyFeeDecimal = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(Profit));
            OnPropertyChanged(nameof(PaidPercentage));
        }
    }

    //ASSUMING AGENCY FEE IS CALCULATED BASED ON FEE AFTER VAT DEDUCTION
    public decimal Profit => FeeExcludingVat - TotalExpenses - (FeeExcludingVat * AgencyFeeDecimal);
    public List<Payment> Payments
    {
        get => Project.Payments;
        set
        {
            Project.Payments = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(PaidAmount));
            OnPropertyChanged(nameof(PaidPercentage));
        }
    }
    public decimal TotalExpectedPaymentsAmount => FeeIncludingVat - (AgencyFeeDecimal * FeeExcludingVat);
    public decimal PaidAmount => Payments.Sum(x => x.Amount);

    //ASSUMING AGENCYFEE IS CALCULATED BASED ON FEE AFTER VAT DEDUCTION
    public decimal PaidPercentage => (PaidAmount / (FeeIncludingVat - (AgencyFeeDecimal * FeeExcludingVat))) * 100m;
    public Project.ProjectStatus Status
    {
        get => Project.Status;
        set
        {
            Project.Status = value;
            OnPropertyChanged();
        }
    }
    public ProjectViewModel(Project Project)
    {
        this.Project = Project;
    }

    public ProjectViewModel(string client, string type, string description, DateTime date, string currency, decimal fee, bool isVAT_Included, decimal vAT_RateDecimal, Agent agent, decimal agencyFeeDecimal, List<Expense> expenses, List<Payment> payments, Project.ProjectStatus status)
    {
        Project = new Project(client, type, description, date, currency, fee, isVAT_Included, vAT_RateDecimal, agent, agencyFeeDecimal, expenses, payments, status);
    }
}
