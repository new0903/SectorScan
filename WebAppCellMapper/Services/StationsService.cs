
using EFCore.BulkExtensions;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.Net;
using System.Runtime.CompilerServices;
using WebAppCellMapper.Data;
using WebAppCellMapper.Data.Models;
using WebAppCellMapper.DTO;
using WebAppCellMapper.Options;

namespace WebAppCellMapper.Services
{
    //public record QueryParams(double? latS, double? latE, double? lonS, double? lonE, double step = GeoBoundsService.EFFECTIVE_STEP);
    //public record QueryResult(int countAdded, string operatorCode, NetworkStandard network,bool isDone,string message);
    //public record QueryProgress(int countSectors, string operatorCode, NetworkStandard network);
    public record OperatorDTO(long Id, string Code);



    public class StationsService : IStationsService
    {
        private readonly AppDBContext context;
        private readonly IGeoBoundsService boundsService;
       private readonly IProxyService proxyService;
        //private readonly IOptions<RequestSettings> options;

        //private readonly IHttpClientFactory clientFactory;
        private readonly ILogger<StationsService> logger;
        private readonly RequestSettings requestSettings;

        //private ConcurrentDictionary<Guid, Task> requests = new ConcurrentDictionary<Guid, Task>();

        private int scannedStations=0;
        private int scannedSector = 0;


        private HashSet<long> idsStations;
        private List<Station> stationsList;
      //  private Stream? responseStream;// совершенно забыл про grpc 
        private ConcurrentQueue<SquareSearch>? coordinates;

        public StationsService(AppDBContext context, IGeoBoundsService boundsService, IProxyService proxyService, IOptions<RequestSettings> options, ILogger<StationsService> logger)
        {

            this.context = context;
            this.boundsService = boundsService;
            this.proxyService = proxyService;
            this.logger = logger;
            requestSettings = options.Value;
            stationsList = new List<Station>();
            idsStations = new HashSet<long>();

        }

        /// <summary>
        /// Поиск станций в секторах
        /// </summary>
        /// <remarks>
        /// Этот метод делает запросы, получает сектора если не указано и записывает найденные станции в бд
        /// </remarks>
        private async IAsyncEnumerable<QueryResult> RunSearch(OperatorDTO op, NetworkStandard ns, [EnumeratorCancellation] CancellationToken ct =default)
        {


            // ConcurrentQueue<SquareSearch> coordinates
            //берем только западную Россию
            //уточнить по поводу координат
          //  await WriteResponse("загрузка координат");
            if (coordinates==null)
            {
                coordinates = boundsService.GetCoordianates();
            }



            while (!coordinates.IsEmpty && !ct.IsCancellationRequested)
            {

                {
                    QueryResult res = new QueryResult(op.Code, ns,  scannedStations, scannedSector, coordinates.Count, "загрузка прокси");
                   yield return res;
                   // await WriteResponse(JsonConvert.SerializeObject(res));
                }
                await proxyService.GetProxies();

                using (CancellationTokenSource cancellationToken = new CancellationTokenSource(TimeSpan.FromSeconds(requestSettings.TimeoutSeconds)))
                //если ставит20 секунд или меньше прокси закончатся 
                // так хотя бы ячеек больше закрою
                {
                    var requests = new List<Task>();
                    int counter= coordinates.Count> requestSettings.MaxConnectionsPerServer ? requestSettings.MaxConnectionsPerServer : coordinates.Count;

                       for (int i = 0; i < counter; i++)
                       {
                            if (coordinates.TryDequeue(out var square))
                            {
                                var task = RequestStations(op, ns, square, true, cancellationToken.Token);
                                requests.Add(task);

                            }
                            else
                            {
                                break;
                            }
                        }
                    

                    await Task.WhenAll(requests);
                }
                {
                    QueryResult res = new QueryResult(op.Code, ns, scannedStations, scannedSector, coordinates.Count, "сохроняю в бд");
                  //  await WriteResponse(JsonConvert.SerializeObject(res));
                    yield return res;
                }

                await BulkSyncStationsAsync(op, ns);


                logger.LogInformation($"секторов осталось {coordinates.Count}");
            }
           

        }




        /// <summary>
        /// Http Запрос 
        /// </summary>
        /// <remarks>
        /// Этот метод делает запросы, записывает результаты запроса в коллекции и если запрос не удачный возвращает сектор обратно в очередь
        /// </remarks>
        private async Task<bool> RequestStations( OperatorDTO op, NetworkStandard ns, SquareSearch sector,bool useP=true, CancellationToken ct=default)
        {
         

            try
            {

                var proxy= proxyService.GetProxy();
                if (proxy == null)
                {
                    if (coordinates != null) coordinates.Enqueue(sector);
                    return false;
                }
                using var handler = new HttpClientHandler()
                {
                    Proxy=new WebProxy(proxy.url),
                    UseProxy= true
                };
                using HttpClient client = new HttpClient(handler);
                client.BaseAddress =new Uri("https://4cells.ru:4444/");
                client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");


                string paramsUrl = $"latStart={sector.latStart.ToString().Replace(",",".")}&latEnd={sector.latEnd.ToString().Replace(",", ".")}&lonStart={sector.lonStart.ToString().Replace(",", ".")}&lonEnd={sector.lonEnd.ToString().Replace(",", ".")}";


                var res = await client.GetAsync($"/api/map/enb/{ns.ToString().ToLower()}/{op.Code}?{paramsUrl}", ct);//https://4cells.ru:4444/
                if (res.IsSuccessStatusCode)
                {
                    proxyService.ReleaseProxy(proxy);
                    string content = await res.Content.ReadAsStringAsync(ct);
                    var stations = JsonConvert.DeserializeObject<List<Station>>(content);
                    if (stations == null) return false;
                    foreach (var item in stations)
                    {

                        if (idsStations.Add(item.Id))
                        {
                            item.OperatorId = op.Id;
                            item.Standard = ns;
                            stationsList.Add(item);
                            Interlocked.Increment(ref scannedStations);
                            //scannedStations++;
                        }

                    }
                    Interlocked.Increment(ref scannedSector);
                 //   scannedSector++;
                    /*детальное сканирование сектора если там выдало много станций
                     например сканируем город, там может быть очень много станций*/
                    if (stations.Count>=300&& coordinates!=null)
                    {
                        //добавим доп квадраты для более детального поиска
                        var detailsCoord=boundsService.GetCoordianates(sector.latStart, sector.latEnd, sector.lonStart, sector.lonEnd, (sector.latEnd-sector.latStart)/2);
                        foreach (var item in detailsCoord)
                        {
                            coordinates.Enqueue(item);
                        }   
                    }

                    return true;
                }
                else
                {
                 //   proxyService.DeleteProxy(proxyAddress);
                    if (coordinates!=null) coordinates.Enqueue(sector);

                    logger.LogError("failed request");
                }

            }
            catch (OperationCanceledException)
            {
               // proxyService.DeleteProxy(proxyAddress);
                if (coordinates != null) coordinates.Enqueue(sector);
                logger.LogError("OperationCanceledException");
            }
            catch (Exception ex)
            {
             //   proxyService.DeleteProxy(proxyAddress);
                if (coordinates != null) coordinates.Enqueue(sector);
                logger.LogError($"Exception\nmessage error: {ex.Message}");


            }
            

            return false;
          
        }
       
        /// <summary>
        /// Сохраняем результат в бд 
        /// </summary>
        /// <remarks>
        /// Этот метод сохроняет результаты в бд. наверное итак понятно
        /// </remarks>
        private async Task BulkSyncStationsAsync(OperatorDTO o, NetworkStandard network, CancellationToken ct=default)//Task<HttpResponseMessage> res,
        {

           if (stationsList!=null&& stationsList.Count>0)
            {
                //foreach (var item in stationsList)
                //{
                //    item.OperatorId = o.Id;
                //    item.Standard = network;
                //}
                await context.BulkInsertOrUpdateAsync(stationsList);
                logger.LogInformation($"добавлено станций {stationsList.Count}");
              //  idsStations.Clear();
                stationsList.Clear();

            }
        }


        /// <summary>
        /// Поиск всех станций, по всем операторам и сетям
        /// </summary>
        /// <remarks>
        /// Этот метод для эндпоинта SSE формата
        /// метод выполняется очень долго, поэтому я решил что лучше будет использовать SSE 
        /// что бы отслеживать прогресс загрузки станций в бд
        /// для тестирования в консоль бразуера вставить вот это js код
        /// var eventSource = new EventSource('https://localhost:7040/api/Stations');
        /// eventSource.onmessage = (event) => {
        /// console.log( event.data);
        /// if (event.data.includes('[DONE]')) {
        /// eventSource.close()
        /// }
        /// };
        /// eventSource.onerror = (error) => {
        ///     console.error('EventSource error:', error);
        ///     eventSource.close()
        /// };
        /// можно и fetch использовать
        /// </remarks>
        public async IAsyncEnumerable<QueryResult> SyncStationsAllAsync([EnumeratorCancellation] CancellationToken ct = default)
        {
            //responseStream = httpStream;
            // await WriteResponse("получаем операторов из бд");
            var operators = await context.operators.
                AsNoTracking().
                Select(o => new OperatorDTO(o.Id, o.InternalCode)).
                ToListAsync();

            foreach (var op in operators)//операторы
            {
                foreach (var network in NSEnumerator.GetNetwork)//тип сети
                {
                    await foreach (var item in RunSearch(op, network, ct))
                    {
                        yield return item;
                    }
                    scannedSector = 0;
                    {

                        QueryResult res = new QueryResult(op.Code, network, scannedStations, scannedSector, coordinates==null?0:coordinates.Count, "сканирование станций завершено", false);

                        yield return res;
                    }
                    idsStations.Clear();
                    coordinates = null;
                }
            }
            {

                QueryResult res = new QueryResult(string.Empty, NetworkStandard.Gsm, scannedStations, scannedSector, 0, "сканирование завершено", true);
                // await WriteResponse(JsonConvert.SerializeObject(res));
                yield return res;
            }

        }


        //old
        // public async IAsyncEnumerable<QueryResult> ScanAreaAsync(Stream httpStream, string operatorCode, NetworkStandard network, double latS, double latE, double lonS, double lonE,double step = GeoBoundsService.EFFECTIVE_STEP, [EnumeratorCancellation] CancellationToken ct=default)

        /// <summary>
        /// Поиск всех станций, по оператору, выбраному типу сети и указной области
        /// </summary>
        /// <remarks>
        /// Этот метод для эндпоинта SSE формата
        /// метод выполняется очень долго, поэтому я решил что лучше будет использовать SSE 
        /// что бы отслеживать прогресс загрузки станций
        /// Указанные координаты делятся на квадраты
        /// Размер квадрата укаывается в запросе step
        /// По умолчанию step =1.4 GeoBoundsService.EFFECTIVE_STEP
        /// для тестирования в консоль бразуера вставить вот это js код
        /// var eventSource = new EventSource('https://localhost:7040/api/Stations/lte/250001?latS=50&latE=60&lonS=80&lonE=121');
        /// eventSource.onmessage = (event) => {
        /// console.log( event.data);
        /// if (event.data.includes('[DONE]')) {
        /// eventSource.close()
        /// }
        /// };
        /// eventSource.onerror = (error) => {
        ///     console.error('EventSource error:', error);
        ///     eventSource.close()
        /// };
        /// </remarks>
        public async IAsyncEnumerable<QueryResult> ScanAreaAsync( string operatorCode, NetworkStandard network, double latS, double latE, double lonS, double lonE,double step = GeoBoundsService.EFFECTIVE_STEP, [EnumeratorCancellation] CancellationToken ct=default)
        {

            // responseStream = httpStream;
            var op = await context.operators.
               AsNoTracking().
               Where(o => o.InternalCode == operatorCode)
               .Select(o => new OperatorDTO(o.Id, o.InternalCode))
               .FirstOrDefaultAsync();

            if (op != null)
            {


                coordinates = boundsService.GetCoordianates(latS, latE, lonS, lonE, step);

                await foreach (var item in RunSearch(op, network, ct))
                {
                    yield return item;

                }
                logger.LogInformation("сканирование завершено");

                idsStations.Clear();
            }
            {

                QueryResult res = new QueryResult(operatorCode, network, scannedStations, scannedSector,0, "сканирование станций завершено", true);
                // await WriteResponse(JsonConvert.SerializeObject(res));
                yield return res;
            }
            //logger.LogInformation($" ScanAreaAsync over");


        }

    }

}
