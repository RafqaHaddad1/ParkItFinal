using ParkIt.Models.Data;

namespace ParkIt.ViewModel
{
    public class UserZonesSubzones
    {
        public IEnumerable<Zone>? Zones { get; set; }
        public IEnumerable<Subzone>? Subzones{ get; set; }
        public Employee Employee { get; set; }

        public string UnHashedPassword { get; set; }
    }
}
