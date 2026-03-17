using HtmlAgilityPack;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.Net;


namespace ConsoleApp1
{
    public record BaseStation(long id, int num, double lat, double lon, int locType, int bsType, string bands, bool isAnchor);

    public record ProxyElement(string url, string type);

    internal class Program
    {
        public static HashSet<long> stationsList = new HashSet<long>();


        public static List<string> proxysList = new List<string>();
        public static ConcurrentDictionary<string,Task> tasks = new ConcurrentDictionary<string, Task>();
        static void Main(string[] args)
        {
             
            Console.WriteLine("Hello, World!");
         //   Task.Run(async ()=>await RunTime());
            Task.Run(RunTime);
            Console.ReadLine();
        }

        public static async Task RunTime()
        {
            if (proxysList.Count<1)
            {
               await GetProxys();
            }
            int iter = 15;
            while (iter>0)
            {

               
                Console.WriteLine("Запущена задача");
                foreach (var proxyAddress in proxysList)
                {
                    if (tasks.Count < proxysList.Capacity)
                    {

                       var token=new CancellationTokenSource(TimeSpan.FromSeconds(30));
                        var guid =Guid.NewGuid().ToString();
                        tasks.TryAdd(guid, RequetsStations(guid,proxyAddress, token.Token));

                    }

                }
                if (tasks.Count >= proxysList.Count)
                {
                    Console.WriteLine("ожидание завершения задач");
                    await Task.WhenAll(tasks.Values);
                    Console.WriteLine(" завершены задачи");
                    Console.WriteLine("найдено станций " + stationsList.Count);
                    await Task.Delay(TimeSpan.FromMilliseconds(100));
                    iter--;
                }
            }
            Console.WriteLine("найдено станций " + stationsList.Count);
        }

        public static async Task RequetsStations(string GUID, string proxyAddress,CancellationToken ct)
        {

           // Console.WriteLine($"Попытка с прокси: {proxyAddress}");
            try
            {
                var proxy = new WebProxy(proxyAddress);

                var handler = new HttpClientHandler
                {
                    Proxy = proxy,
                    UseProxy = true
                };

                //Bearer a262b911-4bd7-4095-8b5f-53e51d515ecc
                using HttpClient client = new HttpClient(handler);
                // чтобы имитировать браузер [citation:1]
                client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");

                
                Console.WriteLine($"Отправляю запрос: {proxyAddress}");
                //lte 4g билайн
                var res = await client.GetAsync("https://4cells.ru:4444/api/map/enb/lte/250099?latStart=32.99023555965106&latEnd=75.6504309974655&lonStart=18.720703125000004&lonEnd=162.77343750000003&random=123", ct);
                    // var response=res.EnsureSuccessStatusCode();

                if (res.IsSuccessStatusCode)
                {
                    string content = await res.Content.ReadAsStringAsync(ct);
                    var stations = JsonConvert.DeserializeObject<List<BaseStation>>(content);
                    if (stations == null) return;
                    foreach (var item in stations)
                    {
                        try
                        {
                            if (!stationsList.Contains(item.id))
                            {
                                stationsList.Add(item.id);
                                //  Console.WriteLine("добавлена станция " + item.id);

                            }
                            else
                            {
                                Console.WriteLine("аналог этой станции есть " + item.id);

                            }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine("аналог этой станции есть " + item.id);
                            }
                        }
                        Console.WriteLine("данные получены с " + proxyAddress);
                }
                else
                {

                    Console.WriteLine("failed request "+ proxyAddress);
                }

            }
            catch (Exception ex)
            {

                proxysList.Remove(proxyAddress);
                Console.WriteLine("ex: "+ex.Message);
            }
            var task = tasks.FirstOrDefault(t => t.Key == GUID);
            if (task.Key != null)
            {
                tasks.TryRemove(task);
            }


        }




        public static async Task GetProxys()
        {
            using HttpClient client = new HttpClient();
            // чтобы имитировать браузер [citation:1]
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
            var res = await client.GetAsync("https://proxymania.su/free-proxy?type=&country=RU&speed=");
            if (res.IsSuccessStatusCode)
            {
                var html = await res.Content.ReadAsStringAsync();
               // Console.WriteLine(html);
                var list = ParseProxyTable(html);
                proxysList.AddRange(list.Select(p=>$"{p.type.ToLower()}://{p.url}"));
                foreach (var item in list)
                {

                    Console.WriteLine($"\nrecord: url {item.url}, type {item.type}, full url {item.type.ToLower()}://{item.url}");
                }
            }

        }
        public static List<ProxyElement> ParseProxyTable(string html)
        {
            var proxies = new List<ProxyElement>();
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            // Находим tbody с id="resultTable"
            var tbody = doc.GetElementbyId("resultTable");

            if (tbody == null)
            {
                Console.WriteLine("Таблица с прокси не найдена!");
                return proxies;
            }

            // Получаем все строки таблицы
            var rows = tbody.SelectNodes(".//tr");

            if (rows == null || rows.Count == 0)
            {
                Console.WriteLine("Строки в таблице не найдены!");
                return proxies;
            }

            foreach (var row in rows)
            {
                var cells = row.SelectNodes(".//td");

                if (cells != null && cells.Count >= 3)
                {
                    // Первая ячейка (индекс 0) - прокси с классом proxy-cell
                    var proxyCell = cells[0];

                    // Третья ячейка (индекс 2) - тип прокси
                    var typeCell = cells[2];

                    if (proxyCell != null && typeCell != null)
                    {
                        string url = proxyCell.InnerText.Trim();
                        string type = typeCell.InnerText.Trim();

                        // Проверяем, что тип действительно содержит значение прокси
                        if (!string.IsNullOrEmpty(type) &&
                            (type.Contains("HTTP") || type.Contains("SOCKS")))
                        {
                            proxies.Add(new ProxyElement(url, type));
                        }
                    }
                }
            }

            return proxies;
        }


    }
}
