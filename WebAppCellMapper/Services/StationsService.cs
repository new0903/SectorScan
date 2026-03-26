
using EFCore.BulkExtensions;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.IO.Compression;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
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
        private readonly IProxyHandlerPoolService handlerPoolService;
     //   private readonly IProxyService proxyService;



        private readonly ILogger<StationsService> logger;
        private readonly RequestSettings requestSettings;


        private int scannedStations=0;
        private int scannedSector = 0;


        private HashSet<long> idsStations;
        private List<Station> stationsList;
      //  private Stream? responseStream;// совершенно забыл про grpc 
        private ConcurrentQueue<SquareSearch>? coordinates;

        public StationsService(AppDBContext context, IGeoBoundsService boundsService, IProxyHandlerPoolService handlerPoolService, 
           // IProxyService proxyService,
            IOptions<RequestSettings> options, ILogger<StationsService> logger)
        {

            this.context = context;
            this.boundsService = boundsService;
            this.handlerPoolService = handlerPoolService;
         //   this.proxyService = proxyService;
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
                await handlerPoolService.InitProxies();//proxyService.GetProxies();

                using (CancellationTokenSource cancellationToken = new CancellationTokenSource(TimeSpan.FromSeconds(requestSettings.TimeoutSeconds)))
                //если ставит20 секунд или меньше прокси закончатся 
                // так хотя бы ячеек больше закрою
                {
                    var requests = new List<Task>();
                    int counter =coordinates.Count;
                    counter= counter > requestSettings.MaxConnectionsPerServer ? requestSettings.MaxConnectionsPerServer : counter;
                    //уловие если меньше 50 то надо делать другой поиск
                    //по дефолту такой стоит поиск
                    if (counter>20)
                    {
                        for (int i = 0; i < counter; i++)
                        {
                            if (coordinates.TryDequeue(out var square))
                            {
                                var task = RequestStations(op, ns, square, cancellationToken.Token);
                                requests.Add(task);

                            }
                            else
                            {
                                break;
                            }
                        }
                    }                  
                    //надо сделать так что бы несколько прокси сканировали 1 и тот же сектор. 
                    //что бы увеличить шанс сканирования.
                    else
                    {
                        //надо сделать так что бы реализовать всю мощь 100 проксей
                        //и сканировать не по 1 квадрату на 100 проксии а на сразу все
                        //т.е. условно 7 квадратов = 7 прокси = Долгий поиск работающих прокси
                        //100 прокси / 7 квадратов = 14 прокси = 1 квадрат шансы выше.

                        int countPerProxy = requestSettings.MaxConnectionsPerServer / counter;
                        
                        while (coordinates.TryDequeue(out var square)) 
                        {
                            if (square.IsScanned) continue;
                            for (int i = 0; i < countPerProxy; i++)
                            {

                                var task = RequestStations(op, ns, square, cancellationToken.Token);
                                requests.Add(task);
                            }
                        }
                        
                    }

  


                    await Task.WhenAll(requests);

                    handlerPoolService.RemoveUnusedProxy();
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


        //using var handler = new HttpClientHandler()
        //{
        //    Proxy=new WebProxy(proxy.url),
        //    UseProxy= true
        //};

        /// <summary>
        /// Http Запрос 
        /// </summary>
        /// <remarks>
        /// Этот метод делает запросы, записывает результаты запроса в коллекции и если запрос не удачный возвращает сектор обратно в очередь
        /// </remarks>
        private async Task<bool> RequestStations( OperatorDTO op, NetworkStandard ns, SquareSearch sector, CancellationToken ct=default)
        {

            var handler = handlerPoolService.GetClientHandler();

            try
            {

                if ( handler == null)
                {
                    logger.LogError("no free proxy");
                    if (coordinates != null && !sector.IsScanned && !coordinates.Contains(sector)) coordinates.Enqueue(sector);
                    return false;
                }

                using HttpClient client = new HttpClient(handler, disposeHandler: false);
                client.BaseAddress =new Uri("https://4cells.ru:4444");


                client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/146.0.0.0 Safari/537.36");
                // Добавляем заголовки для эмуляции AJAX/CORS запроса
                client.DefaultRequestHeaders.Accept.ParseAdd("application/json, text/plain, */*");
                client.DefaultRequestHeaders.Add("Origin", "https://4cells.ru");
                client.DefaultRequestHeaders.Add("Referer", "https://4cells.ru/");
                client.DefaultRequestHeaders.Add("Accept-Language", "ru-RU,ru;q=0.9,en-US;q=0.8,en;q=0.7");
                client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate, br, zstd");
                client.DefaultRequestHeaders.Add("Sec-Ch-Ua", "\"Chromium\";v=\"146\", \"Not-A.Brand\";v=\"24\", \"Google Chrome\";v=\"146\"");
                client.DefaultRequestHeaders.Add("Sec-Ch-Ua-Mobile", "?0");
                client.DefaultRequestHeaders.Add("Sec-Ch-Ua-Platform", "\"Windows\"");
                client.DefaultRequestHeaders.Add("Sec-Fetch-Dest", "empty");
                client.DefaultRequestHeaders.Add("Sec-Fetch-Mode", "cors");
                client.DefaultRequestHeaders.Add("Sec-Fetch-Site", "same-site");
                client.DefaultRequestHeaders.Add("Priority", "u=1, i");

                // https://4cells.ru:4444/api/map/enb/lte/250011?
                string paramsUrl = $"latStart={sector.LatStart.ToString().Replace(",",".")}&latEnd={sector.LatEnd.ToString().Replace(",", ".")}&lonStart={sector.LonStart.ToString().Replace(",", ".")}&lonEnd={sector.LonEnd.ToString().Replace(",", ".")}";


                var res = await client.GetAsync($"/api/map/enb/{ns.ToString().ToLower()}/{op.Code}?{paramsUrl}", ct);//https://4cells.ru:4444/
                if (res.IsSuccessStatusCode)
                {

                    handlerPoolService.ReleaseHandler(handler);
                    sector.IsScanned = true;
                    logger.LogInformation($"success request {res.StatusCode}");
                    //proxyService.ReleaseProxy(proxy);
                    //var bytesResp =await res.Content.ReadAsByteArrayAsync();//попробую в ручную. Так хоть что то приходит
                    //  var content=  DecompressGzip(bytesResp);
                    string content = await res.Content.ReadAsStringAsync(ct);
                    var stations = JsonConvert.DeserializeObject<List<Station>>(content);
                    if (stations == null) return false;
                    logger.LogInformation($"success request {res.StatusCode} {stations.Count}");
                    foreach (var item in stations)
                    {

                        if (idsStations.Add(item.Id))
                        {
                            item.OperatorId = op.Id;
                            item.Standard = ns;
                            stationsList.Add(item);
                            Interlocked.Increment(ref scannedStations);
                        }

                    }
                    Interlocked.Increment(ref scannedSector);
                 //   scannedSector++;
                    /*детальное сканирование сектора если там выдало много станций
                     например сканируем город, там может быть очень много станций*/
                    if (stations.Count>=300&& coordinates!=null)
                    {
                        //добавим доп квадраты для более детального поиска
                        var detailsCoord=boundsService.GetCoordianates(sector.LatStart, sector.LatEnd, sector.LonStart, sector.LonEnd, (sector.LatEnd-sector.LatStart)/2);
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
                    if (coordinates!=null && !coordinates.Contains(sector)) coordinates.Enqueue(sector);
                    //handlerPoolService.RemoveProxy(handler);
                    logger.LogError($"failed request {res.StatusCode}");
                }

            }
            catch (OperationCanceledException)
            {
               // proxyService.DeleteProxy(proxyAddress);
                if (coordinates != null && !sector.IsScanned && !coordinates.Contains(sector)) coordinates.Enqueue(sector);
              //  if (handler != null) handlerPoolService.RemoveProxy(handler);
               logger.LogError("OperationCanceledException");
            }
            catch (Exception ex)
            {
             //   proxyService.DeleteProxy(proxyAddress);
                if (coordinates != null && !sector.IsScanned && !coordinates.Contains(sector)) coordinates.Enqueue(sector);
             //   if (handler != null) handlerPoolService.RemoveProxy(handler);
                logger.LogError($"Exception\nmessage error: {ex.Message}");


            }


            return false;
        }


        //string DecompressGzip(byte[] gzipData)
        //{
        //    using (var compressedStream = new MemoryStream(gzipData))
        //    using (var gzipStream = new GZipStream(compressedStream, CompressionMode.Decompress))
        //    using (var resultStream = new MemoryStream())
        //    {
        //        gzipStream.CopyTo(resultStream);
        //        byte[] decompressedBytes = resultStream.ToArray();
        //        return Encoding.UTF8.GetString(decompressedBytes);
        //    }
        //}


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
                    {

                        QueryResult res = new QueryResult(op.Code, network, scannedStations, scannedSector, coordinates==null?0:coordinates.Count, "сканирование станций завершено", false);

                        yield return res;
                    }
                    idsStations.Clear();
                    coordinates = null;
                    scannedStations = 0;
                    scannedSector = 0;

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
