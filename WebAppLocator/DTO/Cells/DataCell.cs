using Domain.Enums;
using System.Text.Json.Serialization;
using WebAppLocator.Helpers;
namespace WebAppLocator.DTO.Cells
{
    public abstract class DataCell
    {
        [JsonPropertyName("mcc")]
        public long Mcc { get; set; }

        [JsonPropertyName("mnc")]
        public long Mnc { get; set; }



        [JsonPropertyName("signal_strength")]
        public int SignalStrength { get; set; }


        //[JsonIgnore]
        //public virtual double WeightSignal { get {
        //       // var s = Standard==NetworkStandard.Lte?0:35;

        //        return Math.Pow( 10 , (SignalStrength) / 20);//-s
        //    }
        //}

        [JsonIgnore]
        public virtual double DistanceSignal //virtual наверное не надо но вдруг там значения другие надо ставить для lte wcdma и gsm
        {
            get
            {
                /*надо поиграть с значениями.*/
                //var N = 2.0; // Коэффициент затухания (город) 2.0
                //double A = -35;// RSSI на 1 метре 35
                var N = GeoHelper.N; // Коэффициент затухания (город) 2.0
                double A = GeoHelper.A;// RSSI на 1 метре 35
                return Math.Pow(10, (A - SignalStrength) / (10 * N));//
            }
        }


        [JsonIgnore]
        public virtual NetworkStandard Standard { get; }

        [JsonIgnore]
        public virtual long Id { get; }

        public DataCell(long mcc, long mnc, int signalStrength)
        {
            Mcc = mcc;
            Mnc = mnc;
            SignalStrength = signalStrength;
        }

        public DataCell()
        {

        }
    }
}
