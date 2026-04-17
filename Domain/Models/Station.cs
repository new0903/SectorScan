
using Domain.Enums;
using System.Text.Json.Serialization;

namespace Domain.Models
{



    public class Station
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }
        [JsonPropertyName("num")]
        public int Num { get; set; }
        [JsonPropertyName("lat")]
        public double Lat { get; set; }
        [JsonPropertyName("lon")]
        public double Lon { get; set; }
        [JsonPropertyName("locType")]
        public int LocType { get; set; }
        [JsonPropertyName("bsType")]
        public int BsType { get; set; }
        [JsonPropertyName("bands")]
        public string Bands { get; set; }

        [JsonIgnore]
        public long OperatorId { get; set; }
        [JsonIgnore]
        public Operator Operator { get; set; }
        [JsonIgnore]
        public NetworkStandard Standard { get; set; }

        [JsonIgnore]
        public DateTime UpdatedAt { get; set; }= DateTime.UtcNow;
    }
}
