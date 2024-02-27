
using System.Globalization;

namespace MauiApp1.Behaviors;

internal class NumericInputValidationBehavior : Behavior<Entry>
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
        var entry = (Entry)sender;

        if (string.IsNullOrEmpty(e.NewTextValue))
        {
            entry.Text = null;
            return;
        }

        string newString = e.NewTextValue;
        if (newString.Contains('.'))
        {
            newString = newString.Replace('.', ',');
        }

        if (!(decimal.TryParse(newString, NumberStyles.AllowDecimalPoint, CultureInfo.CurrentCulture, out decimal newValue)))
        {
            entry.Text = e.OldTextValue;
        }
        else if (newString != e.NewTextValue)
        {
            entry.Text = newString;
        }
    }
}
