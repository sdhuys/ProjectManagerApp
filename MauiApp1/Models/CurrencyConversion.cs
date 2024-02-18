namespace MauiApp1.Models;

public class CurrencyConversion : Transaction
{
    public string FromCurrency { get; set; }
    public bool IsFromSavings { get; set; }
    public string ToCurrency { get; set; }
    public bool IsToSavings { get; set; }
    public decimal ToAmount { get; set; } 

    public CurrencyConversion(string fromCurrency, bool fromSavings, string toCurrency, bool toSavings, decimal fromAmount, decimal toAmount, DateTime date) : base(fromAmount, date)
    {
        FromCurrency = fromCurrency;
        IsFromSavings = fromSavings;
        ToCurrency = toCurrency;
        IsToSavings = toSavings;
        ToAmount = toAmount;
    }
}
