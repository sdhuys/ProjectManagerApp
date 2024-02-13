namespace MauiApp1.Models;

public class CurrencyConversion
{
    public string FromCurrency { get; set; }
    public bool FromSavings { get; set; }
    public string ToCurrency { get; set; }
    public bool ToSavings { get; set; }
    public decimal FromAmount { get; set; }
    public decimal ToAmount { get; set; } 
    public DateTime Date { get; set; }

    public CurrencyConversion(string fromCurrency, bool fromSavings, string toCurrency, bool toSavings, decimal fromAmount, decimal toAmount, DateTime date)
    {
        FromCurrency = fromCurrency;
        FromSavings = fromSavings;
        ToCurrency = toCurrency;
        ToSavings = toSavings;
        FromAmount = fromAmount;
        ToAmount = toAmount;
        Date = date;
    }
}
