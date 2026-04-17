
using Domain.Enums;
using System.Text.Json.Serialization;

namespace WebAppLocator.DTO.Cells
{
    public class LteDataCell : DataCell
    {

        [JsonPropertyName("tac")]
        public long Tac { get; set; }


        [JsonPropertyName("ci")]
        public long Ci { get; set; }


        [JsonIgnore]
        public override NetworkStandard Standard => NetworkStandard.Lte;

        [JsonIgnore]
        public override long Id
        {
            get
            {

                var num = Ci / 256;
                return long.Parse($"{Mcc}{Mnc.ToString().PadLeft(3, '0')}{num}");
            }
        }
    }
}
