using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MauiApp1.Models
{
    public class Payment
    {
        public decimal Amount { get; set; }
        public DateTime Date { get; set; }

        public Payment(decimal amount, DateTime date)
        {
            Amount = amount;
            Date = date;
        }
    }
}
