using Domain.Enums;

namespace WebAppCellMapper.Data.Enums
{
    public static class NetworkConverter
    {
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
}
