
using System.Text.Json.Serialization;
using WebAppLocator.DTO.Cells;

namespace WebAppLocator.DTO
{

    //генератор моделей https://thesyntaxdiaries.com/tools/json-to-csharp
    public class LocationRequest
    {
        [JsonPropertyName("cell")]
        public CellRequest[]? Cell { get; set; }

        //[JsonPropertyName("wifi")]
        //public WifiRequest[]? Wifi { get; set; }

        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
    public class CellRequest
    {
        [JsonPropertyName("lte")]
        public LteDataCell? Lte { get; set; }

        [JsonPropertyName("gsm")]
        public GSMDataCell? Gsm { get; set; }

        [JsonPropertyName("wcdma")] // в логах почти не встречаются почему то
        public GSMDataCell? Wcdma { get; set; }

        [JsonPropertyName("Nr")]// хз надо ли вообще в яндекс локаторе вообще нет 5g. Надо будет уточнить этот момент.
        public LteDataCell? Nr { get; set; }

        [JsonIgnore]//получаем id станции в бд
        public DataCell? Data=>GetType().GetProperties()
            .Select(p => p.GetValue(this) as DataCell)
            .FirstOrDefault(cell => cell != null);

       

    }



    //public class WifiRequest
    //{
    //    [JsonPropertyName("bssid")]
    //    public string Bssid { get; set; }

    //    [JsonPropertyName("signal_strength")]
    //    public long SignalStrength { get; set; }
    //}
}
