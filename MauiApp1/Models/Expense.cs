using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MauiApp1.Models;

public class Expense
{
    public string Name { get; set; }
    public decimal Amount { get; set; }
    public bool IsRelative { get; set; }
    public decimal RelativeFeeDecimal { get; set; }

    public Expense(string name, bool isRelative, decimal value)
    {
        Name = name;
        IsRelative = isRelative;

        if (isRelative)
            RelativeFeeDecimal = value/100;
        else
            Amount = value;
    }
}
