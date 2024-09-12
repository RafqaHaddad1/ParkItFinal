namespace ParkIt.Models.Data
{
    public class ParkingSpot
    {
        public int SpotID { get; set; }
        public int Subzone_ID { get; set; }
        public bool isAvailable { get; set; }
        public bool isOvercapacity { get; set; }
    }

}
