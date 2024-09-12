using System.ComponentModel.DataAnnotations;

namespace ParkIt.Models.Data
{
    public class Employee
    {
        [Key]
        public int Employee_ID { get; set; }
        public string? Name { get; set; }
        public string? Title { get; set; }
        public bool Active { get; set; }
        public string? Password { get; set; }    
        public string? PhoneNumber { get; set; }
        public string? Address { get; set; }
        public string? EmploymentType { get; set; }
        public string? NFCCode { get; set; }
        public string? Kafeel { get; set; }
        public int? Supervisor_ID { get; set; }
        public string? AdditionalNotes { get; set; }
        public string? Files { get; set; }
        public int? Zone_ID { get; set; }
        public int? Subzone_ID { get; set; }
        public bool? IsDeleted { get; set; }
        public DateTime? AddDate { get; set; }
        public DateTime? DeleteDate { get; set; }

    }


}
