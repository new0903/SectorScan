using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace WebAppCellMapper.DTO.Locator.GPX
{
    public class TrackRequest
    {
        [JsonPropertyName("points")]
        public double[][] Points { get; set; }

        [JsonPropertyName("profile")]
        public string Profile { get; set; }

        [JsonPropertyName("elevation")]
        public bool Elevation { get; set; }

        [JsonPropertyName("instructions")]
        public bool Instructions { get; set; }

        [JsonPropertyName("locale")]
        public string Locale { get; set; }

        [JsonPropertyName("points_encoded")]
        public bool PointsEncoded { get; set; }

        [JsonPropertyName("points_encoded_multiplier")]
        public long PointsEncodedMultiplier { get; set; }

        [JsonPropertyName("details")]
        public string[] Details { get; set; }

        [JsonPropertyName("snap_preventions")]
        public string[] SnapPreventions { get; set; }

        [JsonPropertyName("timeout_ms")]
        public long TimeoutMs { get; set; }

        [JsonPropertyName("alternative_route.max_paths")]
        public long AlternativeRouteMaxPaths { get; set; }

        [JsonPropertyName("algorithm")]
        public string Algorithm { get; set; }
    }
    public class GpxTrackRequest
    {
        [XmlRoot("gpx", Namespace = "http://www.topografix.com/GPX/1/1")]
        public class Gpx
        {
            [XmlElement("trk")]
            public Track Track { get; set; }
        }

        public class Track
        {
            [XmlElement("trkseg")]
            public TrackSegment Segment { get; set; }
        }

        public class TrackSegment
        {
            [XmlElement("trkpt")]
            public List<TrackPoint> Points { get; set; } = new();
        }

        public class TrackPoint
        {
            [XmlAttribute("lat")]
            public double Latitude { get; set; }

            [XmlAttribute("lon")]
            public double Longitude { get; set; }
        }


        //public class TrackPath
        //{
        //    [JsonPropertyName("points")]
        //    public double[][] TrackPoints { get; set; }

        //}
    }
}
