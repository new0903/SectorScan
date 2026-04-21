

using System.Text.Json.Serialization;

namespace WebAppCellMapper.Data.Models
{
    public class Operator
    {
        [JsonIgnore]
        public long Id { get; set; }
        [JsonPropertyName("internalCode")]
        public string InternalCode { get; set; }//в url вставлять
        [JsonPropertyName("name")]
        public string Name { get; set; }
        [JsonPropertyName("visibleCode")]
        public string VisibleCode { get; set; }
   
    }
}
