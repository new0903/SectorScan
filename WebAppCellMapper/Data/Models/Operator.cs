using Newtonsoft.Json;

namespace WebAppCellMapper.Data.Models
{
    public class Operator
    {
        [JsonIgnore]
        public long Id { get; set; }
        [JsonProperty("internalCode")]
        public string InternalCode { get; set; }//в url вставлять
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("visibleCode")]
        public string VisibleCode { get; set; }
   
    }
}
