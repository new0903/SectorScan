using Grpc.Core;
using WebAppCellMapper.Data.Models;
using WebAppCellMapper.Services;

namespace WebAppCellMapper.Grpc
{
    public class StationGRPCService : StationServiceGrpc.StationServiceGrpcBase
    {
        private readonly ILogger<StationGRPCService> logger;
        private readonly IStationsScanningManager scanningManager;

        public StationGRPCService(ILogger<StationGRPCService> logger, IStationsScanningManager scanningManager)
        {
            this.logger = logger;
            this.scanningManager = scanningManager;
        }

   
        public override async Task<ResultResponse> FullScan(Empty request, ServerCallContext context)
        {
            try
            {

                scanningManager.StartFullScan();

                return new ResultResponse()
                {
                    Result = true
                };
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
            }
            return new ResultResponse()
            {
                Result = false
            };
        }
        public override async Task<RequestResponse> Stats(Empty request, ServerCallContext context)
        {
            var item = scanningManager.GetCurrentProcess();
            var response = new RequestResponse
            {
                CountSectors = item.CountSectors,
                ScannedSector = item.CountSectorsScaned,
                CountAdded = item.CountAdded,
                Message = item.Message,
                Network = NSEnumerator.ToGrpcEnum(item.Network),
                OperatorCode = item.OperatorCode,
                Timestamp = item.Timestamp.ToString(),
                IsDone = item.isDone,
            };
            return response;
        }
        public override async Task<ResultResponse> StopProccess(Empty request, ServerCallContext context)
        {
            await scanningManager.StopCurrentProccess();
            return new ResultResponse()
            {
                Result = true
            }; 
        }
        public override async Task<ResultResponse> CanceledProccess(Empty request, ServerCallContext context)
        {
            await scanningManager.CanceledProccess();
            return new ResultResponse()
            {
                Result = true
            };
        }
    }
}
