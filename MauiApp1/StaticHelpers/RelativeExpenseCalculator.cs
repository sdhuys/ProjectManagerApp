using MauiApp1.Models;

namespace MauiApp1.StaticHelpers;

public static class RelativeExpenseCalculator
{
    public static void SetRelativeExpensesAmounts(IEnumerable<ProjectExpense> expenses, decimal expectedProjectEarnings, decimal agencyFeeDecimal, decimal actualProjectEarnings)
    {
        var totalAbsoluteExpenses = expenses.Where(x => !x.IsRelative).Select(x => x.Amount).Sum();
        var expectedAbsoluteAgencyFee = expectedProjectEarnings * agencyFeeDecimal;
        var expectedProfitLeft = expectedProjectEarnings - (totalAbsoluteExpenses + expectedAbsoluteAgencyFee);

        var actualProfitLeft = actualProjectEarnings - totalAbsoluteExpenses;

        foreach (var relExpense in expenses.OfType<ProfitSharingExpense>())
        {
            relExpense.ExpectedAmount = expectedProfitLeft > 0 ? expectedProfitLeft * relExpense.RelativeFeeDecimal : 0;
            relExpense.Amount = actualProfitLeft > 0 ? actualProfitLeft * relExpense.RelativeFeeDecimal : 0;
        }
    }
}
