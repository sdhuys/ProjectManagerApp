namespace MauiApp1.Models;

public static class RelativeExpenseCalculator
{
    public static void SetRelativeExpensesAmounts(IEnumerable<Expense> expenses, decimal projectFee, decimal agencyFeeDecimal)
    {
        var totalAbsoluteExpenses = expenses.Where(x => !x.IsRelative).Select(x => x.Amount).Sum();
        var absoluteAgencyFee = projectFee * agencyFeeDecimal;
        var profitLeft = projectFee - (totalAbsoluteExpenses + absoluteAgencyFee);

        foreach (var relExpense in expenses.Where(x => x.IsRelative))
        {
                relExpense.Amount = profitLeft > 0 ? profitLeft * relExpense.RelativeFeeDecimal : 0;
        }
    }
}
