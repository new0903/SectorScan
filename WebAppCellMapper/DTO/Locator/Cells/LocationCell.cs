namespace WebAppCellMapper.DTO.Locator.Cells
{
    // public record LocationCell(long id,double lat,double lon);
    public class LocationCell
    {
        public long Id { get;  set; }
        public double Lat { get;  set; }
        public double Lon { get;  set; }
        public double WeightSignal { get; set; }
        public double DistanceSignal { get; set; }
        public int SignalStrength { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;


        public LocationCell(long id, double lat, double lon)
        {
            Id=id;
            Lat = lat;
            Lon = lon;
        }
    }
}
