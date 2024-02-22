using MauiApp1.Models;
using MauiApp1.ViewModels;
using System.Reflection;

namespace MauiApp1.Selectors;
internal class SavingsTransactionsTemplateSelector : DataTemplateSelector
{

    public DataTemplate SavingsExpenseTemplate { get; set; }
    public DataTemplate SavingsTransferTemplate { get; set; }
    public DataTemplate IncomingSavingsConversionTemplate { get; set; }
    public DataTemplate OutgoingSavingsConversionTemplate { get; set; }
    protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
    {
        if (container.BindingContext is SpendingOverviewViewModel viewModel)
        {
            if (item is TransferTransaction) return SavingsTransferTemplate;
            else if (item is ExpenseTransaction) return SavingsExpenseTemplate;
            else if (item is CurrencyConversion c && c.ToCurrency == viewModel.SelectedCurrency) return IncomingSavingsConversionTemplate;
            else if (item is CurrencyConversion cc && cc.FromCurrency == viewModel.SelectedCurrency) return OutgoingSavingsConversionTemplate;
        }

        return null;
    }
}