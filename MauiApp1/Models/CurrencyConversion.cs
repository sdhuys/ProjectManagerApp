namespace MauiApp1.Models;

public class CurrencyConversion
{
    public string FromCurrency { get; set; }
    public string ToCurrency { get; set; }
    public decimal FromAmount { get; set; }
    public decimal ToAmount { get; set; } 
    public DateTime Date { get; set; }

    public CurrencyConversion(string fromCurrency, string toCurrency, decimal fromAmount, decimal toAmount, DateTime date)
    {
        FromCurrency = fromCurrency;
        ToCurrency = toCurrency;
        FromAmount = fromAmount;
        ToAmount = toAmount;
        Date = date;
    }
}
