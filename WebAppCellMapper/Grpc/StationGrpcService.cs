using Grpc.Core;
using NetTopologySuite.Index.HPRtree;
using WebAppCellMapper.Data.Models;
using WebAppCellMapper.Services;

namespace WebAppCellMapper.Grpc
{
    public class StationGRPCService : StationServiceGrpc.StationServiceGrpcBase
    {
        private readonly ILogger<StationGRPCService> logger;
        private readonly IStationsScanningManager scanningManager;
        private readonly IStationsService stationsService;

        public StationGRPCService(ILogger<StationGRPCService> logger, IStationsScanningManager scanningManager,IStationsService stationsService)
        {
            this.logger = logger;
            this.scanningManager = scanningManager;
            this.stationsService = stationsService;
        }

        public override async Task SyncStationsAll(Empty request, IServerStreamWriter<RequestResponse> responseStream, ServerCallContext context)
        {
            try
            {

                //не вышел из цикла надо будет посмотреть в чем дело.
                await foreach (var item in stationsService.SyncStationsAllAsync(context.CancellationToken))
                {

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
                    await responseStream.WriteAsync(response, context.CancellationToken);
                    if (item.isDone)
                    {
                        logger.LogInformation("scanned over");
                        return;  // Выходим из цикла
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);

                throw;
            }
        }
        public override async Task ScanAreaStream(RequestParamsForArea request, IServerStreamWriter<RequestResponse> responseStream, ServerCallContext context)
        {
            try
            {

                //не вышел из цикла надо будет посмотреть в чем дело.
                await foreach (var item in stationsService.ScanAreaAsync(request.OperatorCode, NSEnumerator.ToNetworkStandardEnum(request.Network),
                    request.Coordinates.LatS,
                    request.Coordinates.LatE, 
                    request.Coordinates.LonS,
                    request.Coordinates.LonE,
                    request.Step,
                    context.CancellationToken))
                {

                    var response = new RequestResponse
                    {
                        CountSectors = item.CountSectors,
                        ScannedSector = item.CountSectorsScaned,
                        CountAdded = item.CountAdded,
                        Message = item.Message,
                        Network = NSEnumerator.ToGrpcEnum(item.Network),
                        OperatorCode = item.OperatorCode,
                        Timestamp = item.Timestamp.ToString(),
                        IsDone=item.isDone,
                    };
                    await responseStream.WriteAsync(response,context.CancellationToken);
                    if (item.isDone)
                    {
                        logger.LogInformation("scanned over");
                        return;  // Выходим из цикла
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);

                throw;
            }
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
            var item = scanningManager.GetCurrentProccess();
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
                Result = false
            }; 
        }
    }
}
