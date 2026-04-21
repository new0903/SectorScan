
using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text.Json;
using WebAppCellMapper.Data;
using WebAppCellMapper.Data.Enums;
using WebAppCellMapper.Data.Models;
using WebAppCellMapper.Data.Repositories;
using WebAppCellMapper.DTO;
using WebAppCellMapper.Helpers;
using WebAppCellMapper.Options;
using WebAppCellMapper.Proxy;

namespace WebAppCellMapper.Services
{
  
    public class StationsService : IStationsService
    {
        private readonly IProgressRepository progressService;
        private readonly IStationsRepository stationsRepository;
        private readonly IRequestHelper requestHelper;
       // private readonly AppDBContext context;
        private readonly IGeoBoundsService boundsService;
        private readonly IProxyHandlerPoolService handlerPoolService;



        private readonly ILogger<StationsService> logger;
        private readonly RequestSettings requestSettings;

        //   private readonly HttpClientHandler Myhandler;
        private readonly ProxyHandler Myhandler;


        private int scannedStations=0;
        private int scannedSector = 0;

        private ProgressDTO? progress { get; set; } = null;
        //private List<Station> stationsList;
        private ConcurrentQueue<SquareSearch>? coordinates;

        public StationsService(
            IStationsRepository stationsRepository,
            IRequestHelper requestIdGenerator,
         //   AppDBContext context,
            IProgressRepository progressService,
            IGeoBoundsService boundsService,
            IProxyHandlerPoolService handlerPoolService, 
            IOptions<RequestSettings> options,
            ILogger<StationsService> logger)
        {
            this.progressService = progressService;
            this.boundsService = boundsService;
            this.handlerPoolService = handlerPoolService;
            this.logger = logger;
            this.stationsRepository = stationsRepository;
            this.requestHelper = requestIdGenerator;
         //   this.context = context;
            requestSettings = options.Value;
          //  stationsList = new List<Station>();
            var handler = new HttpClientHandler();
            handler.AutomaticDecompression = DecompressionMethods.All;
            Myhandler = new ProxyHandler(handler, handlerPoolService.GetUserAgent());

        }

        private async IAsyncEnumerable<QueryResult> RunSearch( [EnumeratorCancellation] CancellationToken ct =default)
        {


            if (progress==null)
            {
                {
                    QueryResult res = new QueryResult(string.Empty, NetworkStandard.Gsm, scannedStations, scannedSector, 0, "progress null");
                    yield return res;
                }

            }
            else
            {
                if (coordinates == null)
                {
                    var coord = boundsService.GetCoordianates();
                    coordinates = new ConcurrentQueue<SquareSearch>(coord);
                    progress.Coordinates=coord;
                }



                while (!coordinates.IsEmpty && !ct.IsCancellationRequested)
                {



                    {
                        QueryResult res = new QueryResult(progress.Code, progress.Standard, progress.AddedStationsCount, progress.ScannedCount, coordinates.Count, "обновляю прокси");
                        yield return res;
                    }
                    await handlerPoolService.InitProxies();

                    {
                        QueryResult res = new QueryResult(progress.Code, progress.Standard, progress.AddedStationsCount, progress.ScannedCount, coordinates.Count, "Поиск станций");
                        yield return res;
                    }

                    using (CancellationTokenSource cancellationToken = new CancellationTokenSource(TimeSpan.FromSeconds(requestSettings.TimeoutSeconds)))
                    {
                        var requests = new List<Task>();

                        int counter = coordinates.Count;
                        counter = counter > requestSettings.MaxConnectionsPerServer ? Math.Min(requestSettings.MaxConnectionsPerServer,handlerPoolService.CountProxy) 
                            : Math.Min(counter, handlerPoolService.CountProxy);
                        {
                            for (int i = 0; i < counter; i++)
                            {
                                if (coordinates.TryDequeue(out var square))
                                {
                                    var task = RequestStations(square, ct: cancellationToken.Token);
                                    requests.Add(task);

                                }
                                else
                                {
                                    break;
                                }
                            }
                        }
                        {
                            if (coordinates.TryDequeue(out var square))
                            {
                                var task = RequestStations(square, false, ct: cancellationToken.Token);
                                requests.Add(task);
                            }
                        }
                        //ждем завершения всех задач
                        await Task.WhenAll(requests);

                        //очищаем не эффективные прокси
                        handlerPoolService.RemoveUnusedProxy();
                    }   
                    if (!ct.IsCancellationRequested)
                    {
                        progress.Coordinates = coordinates.ToList();
                        progress.AddedStationsCount = scannedStations;
                        progress.ScannedCount = scannedSector;
                        progress.TotalCount = progress.Coordinates.Count;
                    }

                    {
                        QueryResult res = new QueryResult(progress.Code, progress.Standard, progress.AddedStationsCount, progress.ScannedCount, progress.Coordinates.Count, "сохроняю в бд найденные станции");
                        yield return res;
                    }

                    await stationsRepository.BulkSyncStationsAsync(ct);
                  //  await BulkSyncStationsAsync(ct);



                    {
                        QueryResult res = new QueryResult(progress.Code, progress.Standard, progress.AddedStationsCount, progress.ScannedCount, progress.Coordinates.Count, "сохроняю в бд прогресс");
                        yield return res;
                    }
                    await progressService.SaveProgress(progress,ct);

                 
                    await Task.Delay(TimeSpan.FromSeconds(10), ct);
                }

            }


        }



        private async Task<bool> RequestStations(  SquareSearch sector,bool useProxy=true, CancellationToken ct=default)//OperatorDTO op, NetworkStandard ns,
        {
            await Task.Delay(TimeSpan.FromSeconds(Random.Shared.Next(requestSettings.RandomStartRequestSeconds)), ct);

            var handler = useProxy ? handlerPoolService.GetClientHandler() : Myhandler;
            try
            {
                if (handler == null|| handler.IsBan)
                {
                    //if (handler!=null)
                    //{
                    //    handler.UserAgent = handlerPoolService.GetUserAgent();
                    //}
                    logger.LogError("429 ban ip");
                    if (coordinates != null && !sector.IsScanned && !coordinates.Contains(sector)) coordinates.Enqueue(sector);
                    return false;
                }
                //стартовый ID
                var resId= await requestHelper.InitRequest(handler,ct);
                if (!resId)
                {
                    logger.LogInformation($"failed get request id: {handler.LastRequestId}");
                    if (coordinates != null && !sector.IsScanned && !coordinates.Contains(sector)) coordinates.Enqueue(sector);
                    return false;
                }
                await Task.Delay(TimeSpan.FromMilliseconds(500));



                using HttpClient client = requestHelper.GetHttpClient(handler);

                string paramsUrl = $"latStart={sector.LatStart.ToString().Replace(",", ".")}&latEnd={sector.LatEnd.ToString().Replace(",", ".")}&lonStart={sector.LonStart.ToString().Replace(",", ".")}&lonEnd={sector.LonEnd.ToString().Replace(",", ".")}";

                var id = requestHelper.GenerateRequestId($"/api/map/enb/{progress.Standard.ToString().ToLower()}/{progress.Code}?{paramsUrl}", handler.LastRequestId, handler.UserAgent);

                client.DefaultRequestHeaders.Add("x-request-id", id);

                var res = await client.GetAsync($"/api/map/enb/{progress.Standard.ToString().ToLower()}/{progress.Code}?{paramsUrl}", ct);



                if (res.IsSuccessStatusCode)
                {

                    if (useProxy)
                    {

                        handlerPoolService.ReleaseHandler(handler);
                    }
                    sector.IsScanned = true;

                    string content = await res.Content.ReadAsStringAsync(ct);
                    var stations = JsonSerializer.Deserialize<List<Station>>(content);
                    if (stations == null) return false;
                    logger.LogInformation($"success request {res.StatusCode} {stations.Count}, coordinates: LatStart={sector.LatStart}, LatEnd={sector.LatEnd}, LonStart={sector.LonStart}, LonEnd={sector.LonEnd}");
                    foreach (var item in stations)
                    {

                        item.OperatorId = progress.OperatorId;
                        item.Standard = progress.Standard;
                        stationsRepository.StationsList.Add(item);
                        Interlocked.Increment(ref scannedStations);

                    }
                    Interlocked.Increment(ref scannedSector);
                    /*детальное сканирование сектора если там выдало много станций
                     например сканируем город, там может быть очень много станций*/
                    if (stations.Count>=300&& coordinates!=null)
                    {
                        //добавим доп квадраты для более детального поиска
                        var detailsCoord=boundsService.GetCoordianates(sector.LatStart, sector.LatEnd, sector.LonStart, sector.LonEnd, (sector.LatEnd-sector.LatStart)/2);
                        logger.LogInformation($"added sectors  {detailsCoord.Count}");
                        foreach (var item in detailsCoord)
                        {
                            coordinates.Enqueue(item);
                        }   
                    }
                    return true;
                }
                else
                {
                    logger.LogError($"failed request {res.StatusCode}");
                }

            }
            catch (OperationCanceledException)
            {
               logger.LogError("OperationCanceledException"); 
            }
            catch (Exception ex)
            {
                logger.LogError($"Exception\nmessage error: {ex.Message}");
            }
            finally
            {
                if (coordinates != null && !sector.IsScanned && !coordinates.Contains(sector)) coordinates.Enqueue(sector);
                if (handler != null) handler.LastRequestId = string.Empty;
            }


            return false;
        }

        public async IAsyncEnumerable<QueryResult> SyncStationsAllAsync([EnumeratorCancellation] CancellationToken ct = default)
        {
            var operators= await progressService.LoadProgress(ct);

            if (operators.Count==0)
            {
                await progressService.InitProgress();
                operators = await progressService.LoadProgress(ct);
            }
            foreach (var op in operators)
            {
                if (op.Coordinates.Count > 0)
                {
                    coordinates = new ConcurrentQueue<SquareSearch>(op.Coordinates);
                }
                else
                {
                    var coordArr = boundsService.GetCoordianates();
                    coordinates = new ConcurrentQueue<SquareSearch>(coordArr);
                    op.Coordinates = coordArr;
                    op.TotalCount = op.Coordinates.Count;
                }
                
                scannedStations = op.AddedStationsCount;
                scannedSector = op.ScannedCount;

                if (op.Status != ProgressStatus.InProgress)
                {
                    op.Status = ProgressStatus.InProgress;
                    op.StartedAt = DateTime.UtcNow;
                   // op.LastProcessedAt = DateTime.UtcNow;
                    await progressService.SaveProgress(op);
                }
                progress = op;

                await foreach (var item in RunSearch(ct))
                {
                    yield return item;
                }
                {
                    op.Status = ProgressStatus.Completed;
                    op.CompletedAt = DateTime.UtcNow;
                    await progressService.SaveProgress(op, ct);

                    QueryResult res = new QueryResult(op.Code, op.Standard, scannedStations, scannedSector, coordinates == null ? 0 : coordinates.Count, "сканирование станций завершено", false);

                    yield return res;
                }
            }
  
            {

                QueryResult res = new QueryResult(string.Empty, NetworkStandard.Gsm, 0, 0, 0, "сканирование завершено", true);
                yield return res;
            }

        }



    }

}
