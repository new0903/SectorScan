using Azure.Core;
using EFCore.BulkExtensions;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Services;
using NetTopologySuite.Geometries;
using Newtonsoft;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using WebAppCellMapper.Data;
using WebAppCellMapper.Data.Models;
using WebAppCellMapper.DTO;

namespace WebAppCellMapper.Services
{
    //public record QueryParams(double? latS, double? latE, double? lonS, double? lonE, double step = GeoBoundsService.EFFECTIVE_STEP);
    //public record QueryResult(int countAdded, string operatorCode, NetworkStandard network,bool isDone,string message);
    //public record QueryProgress(int countSectors, string operatorCode, NetworkStandard network);
    public record OperatorDTO(long Id, string Code);



    public class StationsService
    {
        private readonly AppDBContext context;
        private readonly GeoBoundsService boundsService;
        private readonly ProxyService proxyService;
        private readonly ILogger<StationsService> logger;

        //private ConcurrentDictionary<Guid, Task> requests = new ConcurrentDictionary<Guid, Task>();

        private int scannedStations=0;
        private int scannedSector = 0;
        private HashSet<long> idsStations;
        private List<Station> stationsList;
      //  private Stream? responseStream;// совершенно забыл про grpc 
        private ConcurrentQueue<SquareSearch>? coordinates;

        public StationsService(AppDBContext context, GeoBoundsService boundsService,ProxyService proxyService, ILogger<StationsService> logger) 
        {
            this.context = context;
            this.boundsService = boundsService;
            this.proxyService = proxyService;
            this.logger = logger;
            stationsList = new List<Station>();
            idsStations = new HashSet<long>();

        }




        ///// <summary>
        ///// Пишем ответ клиенту
        ///// </summary>
        ///// <remarks>
        ///// Этот метод для эндпоинта SSE формата
        ///// метод записывает в поток json, если поток существует
        ///// </remarks>
        //private async Task WriteResponse(string json)
        //{
        //    var message = $"data: {json}, {DateTime.Now:T}\n\n";
        //    //    var bytes = Encoding.UTF8.GetBytes(message);

        //    //    // Используем Response.Body.WriteAsync
        //    //    await Response.Body.WriteAsync(bytes, 0, bytes.Length);
        //    //    await Response.Body.FlushAsync(); // Важно: сбрасываем буфер
        //    if (responseStream == null) return;

        //    var bytes = Encoding.UTF8.GetBytes(message);
        //    await responseStream.WriteAsync(bytes, 0, bytes.Length);
        //    await responseStream.FlushAsync();
        //}


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



            while (coordinates.Count>0 || !ct.IsCancellationRequested)
            {

                {
                    QueryResult res = new QueryResult(op.Code, ns,  scannedStations, scannedSector, coordinates.Count, "загрузка прокси");
                   yield return res;
                   // await WriteResponse(JsonConvert.SerializeObject(res));
                }
                var proxies =await proxyService.GetProxies();

                var requests = new List<Task>();

                foreach (var proxy in proxies)
                {
                    if (requests.Count<150)//лимит
                    {
                        if (coordinates.TryDequeue(out var square))
                        {
                            //await WriteResponse($"отправляем запрос прокси {proxy.url}, сканируемый сектор lons={square.latStart} lone={square.latEnd} lons={square.lonStart} lone={square.lonEnd}");
                            CancellationTokenSource cancellationToken = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                            var task = RequetsStations(proxy.url, op, ns, square, true, cancellationToken.Token);
                            requests.Add(task);
                        }
                        else
                        {
                            break;
                        }
                    }
                    else
                    {

                        break;
                    }
                    

                }
                await Task.WhenAll(requests);

                {
                    QueryResult res = new QueryResult(op.Code, ns, scannedStations, scannedSector, coordinates.Count, "сохроняю в бд");
                  //  await WriteResponse(JsonConvert.SerializeObject(res));
                    yield return res;
                }

                await BulkSyncStationsAsync(op, ns);


                logger.LogInformation($"секторов осталось {coordinates.Count}");
            }
            logger.LogInformation("сканирование завершено");

            {

                QueryResult res = new QueryResult(op.Code, ns, scannedStations, scannedSector, coordinates.Count, "сканирование станций завершено",true);
                // await WriteResponse(JsonConvert.SerializeObject(res));
                yield return res;
            }

        }

        /// <summary>
        /// Http Запрос 
        /// </summary>
        /// <remarks>
        /// Этот метод делает запросы, записывает результаты запроса в коллекции и если запрос не удачный возвращает сектор обратно в очередь
        /// </remarks>
        private async Task<bool> RequetsStations( string proxyAddress, OperatorDTO op, NetworkStandard ns, SquareSearch sector,bool useP=true, CancellationToken ct=default)
        {

            try
            {
                var handler = new HttpClientHandler();
                if (useP)
                {
                    var proxy = new WebProxy(proxyAddress);
                    handler.Proxy=proxy;
                    handler.UseProxy = useP;
                }
                using HttpClient client = new HttpClient(handler);
                // чтобы имитировать браузер 
                client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");


                string paramsUrl = $"latStart={sector.latStart.ToString().Replace(",",".")}&latEnd={sector.latEnd.ToString().Replace(",", ".")}&lonStart={sector.lonStart.ToString().Replace(",", ".")}&lonEnd={sector.lonEnd.ToString().Replace(",", ".")}";

                //  var res = await client.GetAsync("https://4cells.ru:4444/api/map/enb/lte/250099?latStart=32.99023555965106&latEnd=75.6504309974655&lonStart=18.720703125000004&lonEnd=162.77343750000003", ct);
                var res = await client.GetAsync($"https://4cells.ru:4444/api/map/enb/{ns.ToString().ToLower()}/{op.Code}?{paramsUrl}", ct);
                if (res.IsSuccessStatusCode)
                {
                    string content = await res.Content.ReadAsStringAsync(ct);
                    var stations = JsonConvert.DeserializeObject<List<Station>>(content);
                    if (stations == null) return false;
                    foreach (var item in stations)
                    {

                        if (idsStations.Add(item.Id))
                        {
                            stationsList.Add(item);
                            scannedStations++;
                        }

                    }
                    scannedSector++;
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
                    proxyService.DeleteProxy(proxyAddress);
                    if (coordinates!=null) coordinates.Enqueue(sector);

                    logger.LogError("failed request");
                    return false;
                }

            }
            catch (OperationCanceledException)
            {
                proxyService.DeleteProxy(proxyAddress);
                if (coordinates != null) coordinates.Enqueue(sector);
                logger.LogError("OperationCanceledException");
                return false;
            }
            catch (Exception ex)
            {
                proxyService.DeleteProxy(proxyAddress);
                if (coordinates != null) coordinates.Enqueue(sector);
                logger.LogError($"Exception\nmessage error: {ex.Message}");
                return false;


            }
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
                foreach (var item in stationsList)
                {
                    item.OperatorId = o.Id;
                    item.Standard = network;
                }
                await context.BulkInsertOrUpdateAsync(stationsList);
                logger.LogInformation($"добавлено станций {stationsList.Count}");
                idsStations.Clear();
                stationsList.Clear();

            }
        }





        // old
        // public async IAsyncEnumerable<QueryResult> SyncStationsAllAsync(Stream httpStream, [EnumeratorCancellation] CancellationToken ct = default)
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
        public async IAsyncEnumerable<QueryResult> SyncStationsAllAsync( [EnumeratorCancellation] CancellationToken ct = default)
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
                    //await WriteResponse($"сканируем сеть {network.ToString()}, оператор {op.Code}");
                   // await RunSearch(op, network, ct);
                    await foreach (var item in RunSearch(op, network, ct))
                    {
                        yield return item;
                        if (item.isDone)
                        {
                            yield break;
                        }
                    }
                }
            }
        }


        //old
        // public async IAsyncEnumerable<QueryResult> SearchByOperatorAsync(Stream httpStream,string operatorCode, NetworkStandard network, [EnumeratorCancellation] CancellationToken ct = default)
        /// <summary>
        /// Поиск всех станций, по оператору и выбраному типу сети
        /// </summary>
        /// <remarks>
        /// Этот метод для эндпоинта SSE формата
        /// метод выполняется долго(5-10 минут), поэтому я решил что лучше будет использовать SSE 
        /// что бы отслеживать прогресс загрузки станций
        /// для тестирования в консоль бразуера вставить вот это js код
        /// var eventSource = new EventSource('https://localhost:7040/api/Stations/lte/250001');
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
        public async IAsyncEnumerable<QueryResult> SearchByOperatorAsync(string operatorCode, NetworkStandard network, [EnumeratorCancellation] CancellationToken ct = default)
        {

            //logger.LogInformation($" SearchByOperatorAsync start");
            // responseStream = httpStream;
            var op = await context.operators.
               AsNoTracking().
               Where(o=>o.InternalCode== operatorCode)
               .Select(o => new OperatorDTO(o.Id, o.InternalCode))
               .FirstOrDefaultAsync();
            if (op != null)
            {
                await foreach (var item in RunSearch(op, network, ct))
                {
                    yield return item;
                    if (item.isDone)
                    {
                        logger.LogInformation($" SearchByOperatorAsync isDone");
                        yield break;
                    }
                }
            }
            //logger.LogInformation($" over start");

            //RunSearch(op, network, ct);

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

                //logger.LogInformation($" ScanAreaAsync getCoordinates");
                coordinates = boundsService.GetCoordianates(latS, latE, lonS, lonE, step);
                //await RunSearch(op, network, ct);
                await foreach (var item in RunSearch(op, network, ct))
                {
                    yield return item;
                    if (item.isDone)
                    {
                      //  logger.LogInformation($" ScanAreaAsync isDone");
                        yield break;
                    }
                }
            }
            //logger.LogInformation($" ScanAreaAsync over");


        }

        /// <summary>
        /// Поиск всех станций, по оператору, выбраному типу сети и указной области
        /// </summary>
        /// <remarks>
        /// Создается простой запрос, который получает координаты с целевого сайта. 
        /// Можно использовать прокси, если получен временный бан(429) по ip
        /// </remarks>
        public async Task<QueryResult> SearchAtLocationAsync(double latS, double latE, double lonS, double lonE, string operatorCode, NetworkStandard network,bool useProxy=false,CancellationToken ct=default)
        {
            var op = await context.operators.
               AsNoTracking().
               Where(o => o.InternalCode == operatorCode)
               .Select(o => new OperatorDTO(o.Id, o.InternalCode))
               .FirstOrDefaultAsync();
            if (op == null) return null;
            var sector = new SquareSearch(latS, latE, lonS, lonE);
         
            if (useProxy)
            {
                var proxies = await proxyService.GetProxies();
                bool resRequst = false;
                while (!resRequst)
                {
                    var proxy = proxies.FirstOrDefault();
                    if (proxy == null) continue;
                     resRequst=await RequetsStations(proxy.url, op, network, sector, useProxy, ct);
                    if (!resRequst) proxyService.DeleteProxy(proxy.url);

                }
            }
            else
            {
                await RequetsStations(string.Empty, op, network, sector, useProxy, ct);

            }



            QueryResult res = new QueryResult(op.Code, network, scannedStations, scannedSector, coordinates==null?0:coordinates.Count, string.Empty);
            await BulkSyncStationsAsync(op, network, ct);

            return res;
        }

    }

}
