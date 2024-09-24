using MauiApp1.Models;

namespace MauiApp1.StaticHelpers;

public static class RelativeExpenseCalculator
{
    public static void SetRelativeExpensesAmounts(IEnumerable<ProjectExpense> expenses, IEnumerable<ProfitSharingExpense> profitSharingExpenses, decimal expectedProjectEarnings, decimal agencyFeeDecimal, decimal actualProjectEarnings)
    {
        var totalAbsoluteExpenses = expenses.Sum(x => x.Amount);
        var expectedAbsoluteAgencyFee = expectedProjectEarnings * agencyFeeDecimal;
        var expectedProfitLeft = expectedProjectEarnings - (totalAbsoluteExpenses + expectedAbsoluteAgencyFee);

        var actualProfitLeft = actualProjectEarnings - totalAbsoluteExpenses;

        foreach (var relExpense in profitSharingExpenses)
        {
            relExpense.ExpectedAmount = expectedProfitLeft > 0 ? expectedProfitLeft * relExpense.RelativeFeeDecimal : 0;
            relExpense.Amount = actualProfitLeft > 0 ? actualProfitLeft * relExpense.RelativeFeeDecimal : 0;
        }
    }
}
