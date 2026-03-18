using Grpc.Core;
using NetTopologySuite.Index.HPRtree;
using WebAppCellMapper.Data.Models;
using WebAppCellMapper.Services;

namespace WebAppCellMapper.Grpc
{
    public class StationServiceGRPC : StationServiceGrpc.StationServiceGrpcBase
    {
        private readonly ILogger<StationServiceGRPC> logger;
        private readonly StationsService stationsService;

        public StationServiceGRPC(ILogger<StationServiceGRPC> logger,StationsService stationsService)
        {
            this.logger = logger;
            this.stationsService = stationsService;
        }

        public override async Task SyncStationsAllStream(Empty request, IServerStreamWriter<RequestResponse> responseStream, ServerCallContext context)
        {
            try
            {
                await foreach (var item in stationsService.SyncStationsAllAsync(context.CancellationToken))
                {

                    var response = new RequestResponse
                    {
                        CountSectors = item.CountSectors,
                        ScannedSector=item.CountSectorsScaned,
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
                        return;  // Выходим из цикла
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);

                throw;
            }
           

            // return base.SyncStationsAllStream(request, responseStream, context);
        }
        public override async Task SearchByOperatorStream(RequestParamsForOperator request, IServerStreamWriter<RequestResponse> responseStream, ServerCallContext context)
        {
            try
            {
                await foreach (var item in stationsService.SearchByOperatorAsync(request.OperatorCode, NSEnumerator.ToNetworkStandardEnum(request.Network), context.CancellationToken))
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
        public override async Task<RequestResponse> SearchAtLocationStream(RequestParamsForLocation request, ServerCallContext context)
        {
            try
            {
                var item = await stationsService.SearchAtLocationAsync(request.Coordinates.LatS,
                    request.Coordinates.LatE,
                    request.Coordinates.LonS,
                    request.Coordinates.LonE, request.OperatorCode, NSEnumerator.ToNetworkStandardEnum(request.Network), request.UseProxy, context.CancellationToken);
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
            catch (Exception ex)
            {

                logger.LogError(ex.Message);
                throw;
            }

        }
    }
}
