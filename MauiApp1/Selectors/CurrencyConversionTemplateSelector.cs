using MauiApp1.Models;
using MauiApp1.ViewModels;

namespace MauiApp1.Selectors;
public class CurrencyConversionTemplateSelector : DataTemplateSelector
{
    public DataTemplate DefaultTemplate { get; set; }
    public DataTemplate FromCurrentCurrencyTemplate { get; set; }
    public DataTemplate ToCurrentCurrencyTemplate { get; set; }

    protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
    {
        if (item is CurrencyConversion conversion && container.BindingContext is SpendingOverviewViewModel viewModel)
        {
            if (conversion.FromCurrency == viewModel.SelectedCurrency)
                return FromCurrentCurrencyTemplate;

            if (conversion.ToCurrency == viewModel.SelectedCurrency)
                return ToCurrentCurrencyTemplate;
        }

        return DefaultTemplate;
    }
}
