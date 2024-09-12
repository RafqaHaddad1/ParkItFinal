using System.ComponentModel.DataAnnotations;

namespace ParkIt.Models.Data
{
    public class Zone
    {
        [Key]
        public int Zone_ID { get; set; }
        public string Zone_Name { get; set; }
        public string Area { get; set; }
        public string Street { get; set; }
        public bool Active { get; set; }
        public string? AllCoordinates { get; set; }
        public int? NumberOfSubzone { get; set; }
        public int? NumberOfRunner { get; set; }
        public int? Supervisor_ID { get; set; }
        public bool? IsDeleted { get; set; }
        public DateTime? AddDate{ get; set; }
        public DateTime? DeleteDate { get; set; }
    }

}
