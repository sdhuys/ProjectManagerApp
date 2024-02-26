using System.Text.Json.Serialization;

namespace MauiApp1.Models;

public abstract class Transaction
{
    public const string LossExpenseDescription = "Overall Loss Coverage";
    public const string SavingsGoalTransferSource = "Savings Goal Portion";
    public const string SpendingsOverdraftExpenseDescription = "Spendings Overdraft Coverage";

    // returns true for transactions that have been automatically created on expenses finalisation
    // returns false for transfers to savings manually added after finaisation
    public bool IsFinalisationTransaction
    {
        get
        {
            if (this is ExpenseTransaction e && (e.Description == LossExpenseDescription || e.Description == SpendingsOverdraftExpenseDescription)) return true;
            if (this is TransferTransaction t && t.Source == SavingsGoalTransferSource) return true;
            return false;
        }
    }
    public decimal Amount { get; set; }
    public DateTime Date { get; set; }

    public Transaction(decimal amount, DateTime date)
    {
        Amount = amount;
        Date = date;
    }
}

public class ExpenseTransaction : Transaction
{
    // Currently only Description setting on UI available for Savings Spending
    public string Description { get; set; }
    public ExpenseTransaction(decimal amount, DateTime date, string description) : base(amount, date)
    {
        Description = description;
    }
}

public class TransferTransaction : Transaction
{
    // Source can be a SpendingCategory.Name, "Savings Goal Portion", or [any description designating external source]
    public string Id { get; set; }
    public string Source { get; set; }
    public string Destination { get; set; }

    // Store existing Transfers for custom JsonConverter to return in case of deserialising existing one
    private static Dictionary<string, TransferTransaction> existingTransfers = new();

    public TransferTransaction(string source, decimal amount, DateTime date, string destination) : base(amount, date)
    {
        Id = Guid.NewGuid().ToString();
        Source = source;
        Destination = destination;
    }

    [JsonConstructor]
    public TransferTransaction(string id, string source, decimal amount, DateTime date, string destination) : base(amount, date)
    {
        Id = id;
        Source = source;
        Destination = destination;

        if (!existingTransfers.ContainsKey(Id))
        {
            existingTransfers[Id] = this;
        }
    }

    public static TransferTransaction GetExistingTransfer(string id)
    {
        if (existingTransfers.ContainsKey(id))
        {
            return existingTransfers[id];
        }
        return null;
    }
}
