using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MauiApp1.Models;

public class Agent
{
    public string Name { get; set; }
    public decimal FeeDecimal { get; set; }

    public Agent(string name, decimal feePercent)
    {
        Name = name;
        FeeDecimal = feePercent / 100m;
    }
}
