using System.ComponentModel.DataAnnotations;
namespace ParkIt.Models.Data
{
    public class Subzone
    {
        [Key]
        public int Subzone_ID { get; set; }
        public int? Zone_ID { get; set; }
        public string Subzone_Name { get; set; }
        public int Capacity { get; set; }
        public bool? IsDeleted { get; set; }
        public DateTime? AddDate { get; set; }
        public DateTime? DeleteDate { get; set; }
    }

}
