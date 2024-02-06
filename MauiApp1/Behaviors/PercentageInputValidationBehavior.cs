using System.Globalization;

namespace MauiApp1.Behaviors;

internal class PercentageInputValidationBehavior : Behavior<Entry>
{
    protected override void OnAttachedTo(Entry entry)
    {
        entry.TextChanged += OnEntryTextChanged;
        base.OnAttachedTo(entry);
    }

    protected override void OnDetachingFrom(Entry entry)
    {
        entry.TextChanged -= OnEntryTextChanged;
        base.OnDetachingFrom(entry);
    }

    private void OnEntryTextChanged(object sender, TextChangedEventArgs e)
    {
        if (String.IsNullOrEmpty(e.NewTextValue))
        {
            ((Entry)sender).Text = "0";
            return;
        }

        if (!(decimal.TryParse(e.NewTextValue, NumberStyles.AllowDecimalPoint, CultureInfo.CurrentCulture, out decimal newValue) && newValue >= 0 && newValue < 100))
        {
            ((Entry)sender).Text = e.OldTextValue;
        }
    }
}
