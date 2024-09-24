using System.ComponentModel.DataAnnotations;

namespace ParkIt.Models.Data
{
    public class Transactions
    {
        [Key]
        public int Transaction_ID { get; set; }
        public string? CarModel { get; set; }
        public DateTime? ArrivalTime { get; set; }
        public DateTime? DispatchTime { get; set; }
        public int Runner_Collect_ID { get; set; }
        public int Runner_Dispatch_ID { get; set; }
        public int Zone_ID { get; set; }
        public int ParkingSpot_ID { get; set; }
        public string? Type { get; set; }
        public int? TicketNumber { get; set; }
        public int? Fee { get; set; } 
        public string? Status { get; set; }
        public string? PhoneNumber { get; set; }
        public double? Rating { get; set; }
        public string? FileName { get; set; }
        public string? Note { get; set; }
      
       public DateTime? AddDate { get; set; }
    }

}
