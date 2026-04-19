
using Domain.Enums;
using System.Linq;
using System.Text.Json.Serialization;

namespace WebAppLocator.DTO.Cells
{
    public class GSMDataCell : DataCell
    {
        public GSMDataCell() { }

        public GSMDataCell(long mcc, long mnc, int signalStrength) : base(mcc, mnc, signalStrength)
        {
        }
        public GSMDataCell(long mcc, long mnc, int signalStrength, long lac, long cid) : base(mcc, mnc, signalStrength)
        {
            Lac = lac;
            Cid = cid;
        }
        [JsonPropertyName("lac")]
        public long Lac { get; set; }

        [JsonPropertyName("cid")]
        public long Cid { get; set; }


        [JsonIgnore]
        public override NetworkStandard Standard => NetworkStandard.Gsm; // стоит ли создовать класс для 3G, надо будет подумать
        //в яндекс локаторе почти ничем не отличается

        [JsonIgnore]
        public override long Id
        {
            get
            {
                //250001 77661 4(cid)
                //250001 77866 8(cid)
                //250001 06603 330 (num/cid)
                //250001 07482 3451(num/cid)
                //250001 07482 345(num/cid)
                // codeOP+Lac(5 char)+Cid(1-4 char)
                var stCid = Cid.ToString();
                var c = stCid.Substring(0,stCid.Length-1);
                var l = Lac.ToString().PadLeft(5, '0');
                return long.Parse($"{Mcc}{Mnc.ToString().PadLeft(3, '0')}{l}{c}");
            }
        }
    }
}
