using Newtonsoft.Json;

namespace MauiApp1.Models
{
    public class Payment
    {
        public decimal Amount { get; set; }
        public DateTime Date { get; set; }
        [JsonIgnore]
        public string AssociatedProjectID { get; set; }

        //Constructor to be called when adding payment to existing project
        public Payment(decimal amount, DateTime date, string projectID)
        {
            Amount = amount;
            Date = date;
            AssociatedProjectID = projectID;
        }

        //Constructor to be called when adding payment to new to be created project (no ID created yet)
        [JsonConstructor]
        public Payment(decimal amount, DateTime date)
        {
            Amount = amount;
            Date = date;
        }
    }
}
