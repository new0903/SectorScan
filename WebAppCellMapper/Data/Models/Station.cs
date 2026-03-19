using Newtonsoft.Json;
using System.Collections;

namespace WebAppCellMapper.Data.Models
{

    public enum NetworkStandard
    {
        Gsm,//2g
        Wcdma,//3g
        Lte,//4g
        Nr,//5g
    }

    public static class NSEnumerator
    {

        public static IEnumerable<NetworkStandard> GetNetwork
        {
                get
                {
                    yield return NetworkStandard.Gsm;
                    yield return NetworkStandard.Wcdma;
                    yield return NetworkStandard.Lte;
                    yield return NetworkStandard.Nr; 
                }
        }

        public static NetworkStandard ToNetworkStandardEnum(NetworkStandardGRPC network)
        {
            return network switch
            {
                NetworkStandardGRPC.Gsm => NetworkStandard.Gsm,
                NetworkStandardGRPC.Wcdma => NetworkStandard.Wcdma,
                NetworkStandardGRPC.Lte => NetworkStandard.Lte,
                NetworkStandardGRPC.Nr => NetworkStandard.Nr,
            };
        }


        public static NetworkStandardGRPC ToGrpcEnum(NetworkStandard network)
        {
            return network switch
            {
                NetworkStandard.Gsm => NetworkStandardGRPC.Gsm,
                NetworkStandard.Wcdma => NetworkStandardGRPC.Wcdma,
                NetworkStandard.Lte => NetworkStandardGRPC.Lte,
                NetworkStandard.Nr => NetworkStandardGRPC.Nr,
            };
        }

    }


    //    public record BaseStation(long id,int num,double lat,double lon,int locType,int bsType,string bands,bool isAnchor);
    public class Station
    {
        [JsonProperty("id")]
        public long Id { get; set; }
        [JsonProperty("num")]
        public int Num { get; set; }
        [JsonProperty("lat")]
        public double Lat { get; set; }
        [JsonProperty("lon")]
        public double Lon { get; set; }
        [JsonProperty("locType")]
        public int LocType { get; set; }
        [JsonProperty("bsType")]
        public int BsType { get; set; }
        [JsonProperty("bands")]
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
