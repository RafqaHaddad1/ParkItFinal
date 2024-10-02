using System.ComponentModel.DataAnnotations;

namespace ParkIt.Models.Data
{
    public class Admin
    {
        [Key]
        public int Admin_ID { get; set; }
        public string? Admin_Name { get; set; }
        public string? Email { get; set; }
        public string? Password { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Address { get; set; }
        public string? Notes { get; set; }
        public string? Files { get; set; }
        public bool? IsDeleted { get; set; }
        public DateTime? AddDate { get; set; }
        public DateTime? DeleteDate { get; set; }
        public DateTime? UpdateDate { get; set; }
        public string? Access { get; set; }
    }
}
