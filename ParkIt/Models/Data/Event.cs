using System.ComponentModel.DataAnnotations;

namespace ParkIt.Models.Data
{
    public class Event
    {
        [Key]
        public int EventID { get; set; }
        //public string Employee_Name { get; set; }
        public int Employee_ID { get; set; }
        public int Zone_ID{ get; set; }
        public string Description { get; set; }
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public string ThemeColor { get; set; }
    }
}
