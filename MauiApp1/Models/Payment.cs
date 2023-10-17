using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MauiApp1.Models
{
    public class Payment
    {
        public decimal Amount { get; set; }
        public DateTime Date { get; set; }
        [JsonIgnore]
        public Guid AssociatedProjectID { get; set; }

        //Constructor to be called when adding payment to existing project
        public Payment(decimal amount, DateTime date, Guid projectID)
        {
            Amount = amount;
            Date = date;
            AssociatedProjectID = projectID;
            PaymentManager.AddPayment(this);
        }

        //Constructor to be called when adding payment to new project (no ID created yet)
        [JsonConstructor]
        public Payment(decimal amount, DateTime date)
        {
            Amount = amount;
            Date = date;
            Debug.WriteLine("PAYMENT CREATING");
            PaymentManager.AddPayment(this);
        }
    }
}
