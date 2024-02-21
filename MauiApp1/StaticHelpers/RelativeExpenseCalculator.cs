using MauiApp1.Models;

namespace MauiApp1.StaticHelpers;

public static class RelativeExpenseCalculator
{
    public static void SetRelativeExpensesAmounts(IEnumerable<ProjectExpense> expenses, decimal projectEarnings, decimal agencyFeeDecimal)
    {
        var totalAbsoluteExpenses = expenses.Where(x => !x.IsRelative).Select(x => x.Amount).Sum();
        var absoluteAgencyFee = projectEarnings * agencyFeeDecimal;
        var profitLeft = projectEarnings - (totalAbsoluteExpenses + absoluteAgencyFee);

        foreach (var relExpense in expenses.Where(x => x.IsRelative))
        {
            relExpense.Amount = profitLeft > 0 ? profitLeft * relExpense.RelativeFeeDecimal : 0;
        }
    }
}
