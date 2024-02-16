namespace MauiApp1.Models;

public class CurrencyConversion
{
    public string FromCurrency { get; set; }
    public bool IsFromSavings { get; set; }
    public string ToCurrency { get; set; }
    public bool IsToSavings { get; set; }
    public decimal FromAmount { get; set; }
    public decimal ToAmount { get; set; } 
    public DateTime Date { get; set; }

    public CurrencyConversion(string fromCurrency, bool fromSavings, string toCurrency, bool toSavings, decimal fromAmount, decimal toAmount, DateTime date)
    {
        FromCurrency = fromCurrency;
        IsFromSavings = fromSavings;
        ToCurrency = toCurrency;
        IsToSavings = toSavings;
        FromAmount = fromAmount;
        ToAmount = toAmount;
        Date = date;
    }
}
