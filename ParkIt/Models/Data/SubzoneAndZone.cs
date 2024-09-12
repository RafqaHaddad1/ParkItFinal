namespace ParkIt.Models.Data
{
    public class SubzoneAndZone
    {
        public Zone zone {  get; set; }
        public IEnumerable<Subzone> Subzones { get; set; }
    }
}
