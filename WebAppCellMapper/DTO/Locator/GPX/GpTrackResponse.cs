
using System.Text.Json.Serialization;

namespace WebAppCellMapper.DTO.Locator.GPX
{
    public class GpTrackResponse
    {
        [JsonPropertyName("paths")]
        public TrackResponsePath[] Paths { get; set; }
    }
    public  class TrackResponsePath
    {
        [JsonPropertyName("points")]
        public TrackResponsePoints Points { get; set; }

    }
    public  class TrackResponsePoints
    {
        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("coordinates")]
        public double[][] Coordinates { get; set; }
    }
}
