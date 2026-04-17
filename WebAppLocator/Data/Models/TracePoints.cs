namespace WebAppLocator.Data.Models
{
    public class TracePoints
    {
        public long Id { get; set; }
        public double Lat { get; set; }
        public double Lon { get; set; }
        public double Accuracy { get; set; }
        public string DeviceId { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public DateTime? DeleteAt { get; set; }

    }
}
