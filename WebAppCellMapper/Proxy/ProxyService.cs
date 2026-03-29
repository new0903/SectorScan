
using HtmlAgilityPack;
using Microsoft.Extensions.Options;
using NetTopologySuite.Index.HPRtree;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using WebAppCellMapper.Options;
//using HtmlAgilityPack;


namespace WebAppCellMapper.Proxy
{
    public record Geolocation(string country,string city);
    public record Proxifly(string proxy,string protocol,string ip,int port,bool https,string anonymity,int score,Geolocation geolocation);
   

    public record ProxyElement(string ip, string protocol,string url);
    public class ProxyService : IProxyService
    {
        private readonly ILogger<ProxyService> logger;


        private readonly RequestSettings requestSettings;

        private ConcurrentStack<ProxyElement> ProxysList { get; set; }
        public DateTime LastUpdate {  get;private set; }


        public ProxyService(ILogger<ProxyService> logger, IOptions<RequestSettings> options) {
            ProxysList=new ConcurrentStack<ProxyElement>();
            requestSettings = options.Value;
            this.logger = logger;
        }

       
        public int CountProxy=>ProxysList.Count;

        //IEnumerable<ProxyElement> 
        public async Task GetProxies()
        {
            if (LastUpdate+TimeSpan.FromMinutes(requestSettings.TimeoutUpdateProxy) <DateTime.UtcNow)
            {
                await GetProxiesRequest();//надо подумать про бд
                //await GetProxiesRequestV2();
                //await GetProxiesRequest();// старые прокси может пригодятся
               // await GetProxiesRequestV1();
            }
           

        }



        public ProxyElement? GetProxy()
        {
            try
            { 
                //if (LastUpdate + TimeSpan.FromMinutes(60) < DateTime.UtcNow || ProxysList.Count < 10)//минимум 10 проксей
                //{
                //    await GetProxiesRequest();
                //    //requestUpdateProxy = true;
                //}
                if (ProxysList.TryPop(out var proxy))
                {
                    return proxy;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
            }
            return null;
        }


        public void ReleaseProxy(ProxyElement proxy)
        {
            //proxyUsed.TryAdd(proxy.url, proxy);
            try
            {

                //var proxyEl = proxyUsed[proxy];
                //if (proxyEl != null)
                //{

                    ProxysList.Push(proxy);
               // }
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);


            }

        }
        private List<ProxyElement> ParseProxyTable(string html)
        {
            var proxies = new List<ProxyElement>();
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var tbody = doc.GetElementbyId("resultTable");

            if (tbody == null)
            {
                return proxies;
            }

            var rows = tbody.SelectNodes(".//tr");

            if (rows == null || rows.Count == 0)
            {
                return proxies;
            }

            foreach (var row in rows)
            {
                var cells = row.SelectNodes(".//td");

                if (cells != null && cells.Count >= 3)
                {
                    var proxyCell = cells[0];

                    var typeCell = cells[2];

                    if (proxyCell != null && typeCell != null)
                    {
                        string ipProxy = proxyCell.InnerText.Trim();
                        string type = typeCell.InnerText.Trim();

                        // Проверяем, что тип действительно содержит значение прокси
                        string protocol = type.ToLower();
                        if (!string.IsNullOrEmpty(type))
                        {
                            if (type.Contains("HTTP"))
                            {
                                protocol = "http";
                            }
                            var proxy = new ProxyElement(ipProxy, type, $"{protocol}://{ipProxy}");
                            if (!ProxysList.Where(p=>p.url== proxy.url).Any())
                            {
                                ProxysList.Push(proxy);
                                proxies.Add(proxy);
                            }
                        }
                    }
                }
            }

            return proxies;
        }


        public async Task GetProxiesRequest()
        {
            LastUpdate = DateTime.UtcNow;
            using HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");

            int i = 1;
            while (true)
            {
                var res = await client.GetAsync($"https://proxymania.su/free-proxy?page={i}");
                if (res.IsSuccessStatusCode)
                {
                    var html = await res.Content.ReadAsStringAsync();
                    var list = ParseProxyTable(html);
                    if (list != null && list.Count > 0)
                    {
                        i++;
                    }
                    else
                    {
                        break;
                    }
                }
            }

            
        }

        public async Task GetProxiesRequestV2()
        {
            LastUpdate = DateTime.UtcNow;
            using HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");

            int i = 1;
            while (true)
            {
                var res = await client.GetAsync($"https://proxyverity.com/free-proxy-list/?limit=25&sort_by=last_checked&sort_order=desc&proxy_page={i}");
                if (res.IsSuccessStatusCode)
                {
                    var html = await res.Content.ReadAsStringAsync();
                    var list = ParseHTML(html);
                    if (list != null && list.Count > 0)
                    {
                        foreach (var item in list)
                        {
                            ProxysList.Push(item);
                        }

                        i++;
                    }
                    else
                    {
                        break;
                    }
                }
            }
            //https://proxyverity.com/free-proxy-list/?limit=25&sort_by=last_checked&sort_order=desc&country_code=&type=&anonymity=
        }






        private List<ProxyElement> ParseHTML(string html)
        {
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(html);
            List<ProxyElement> proxies = new List<ProxyElement>();

            // 2. Находим таблицу с нужным классом
            //    Используем XPath для поиска элемента <table> с атрибутом class, содержащим 'table-striped'
            HtmlNode table = doc.DocumentNode.SelectSingleNode("//table[contains(@class, 'table-striped')]");

            if (table == null)
            {
                Console.WriteLine("Таблица не найдена!");
                return proxies;
            }

            // 3. Получаем все строки тела таблицы (<tbody> -> <tr>)
            //    Игнорируем заголовки, берем только строки из <tbody>
            HtmlNodeCollection rows = table.SelectNodes(".//tbody/tr");

            if (rows == null || rows.Count == 0)
            {
                return proxies;
            }


            // 4. Проходим по каждой строке и извлекаем данные
            foreach (HtmlNode row in rows)
            {
                // Получаем все ячейки (<td>) в строке
                HtmlNodeCollection cells = row.SelectNodes("td");
                if (cells == null || cells.Count < 8) // Проверяем, что ячеек достаточно
                    continue;

                // --- Извлекаем IP Address:Port (первая ячейка) ---
                // В первой ячейке находится <a> с текстом
                HtmlNode ipLink = cells[0].SelectSingleNode(".//a");
                string ipPort = ipLink != null ? ipLink.InnerText.Trim() : cells[0].InnerText.Trim();

                // --- Извлекаем Type (третья ячейка, индекс 2) ---
                // В ячейке типа находится <a> с текстом типа прокси
                HtmlNode typeLink = cells[2].SelectSingleNode(".//a");
                string type = typeLink != null ? typeLink.InnerText.Trim() : cells[2].InnerText.Trim();

                // --- Извлекаем Anonymity (четвертая ячейка, индекс 3) ---
                string anonymity = cells[3].InnerText.Trim();

                // --- Извлекаем Response (седьмая ячейка, индекс 7) ---
                // В ячейке Response находится <span> с текстом значения
                HtmlNode responseSpan = cells[7].SelectSingleNode(".//span");
                string response = responseSpan != null ? responseSpan.InnerText.Trim() : cells[7].InnerText.Trim();

                var proxy = new ProxyElement(ipPort, type, $"{type.ToLower()}://{ipPort}");
                proxies.Add(proxy);
                // Создаем объект и добавляем в список
                //proxies.Add(new ProxyInfo
                //{
                //    IpPort = ipPort,
                //    Type = type,
                //    Anonymity = anonymity,
                //    Response = response
                //});
            }
            return proxies;
        }




        //это интересные прокси но палевные и не надежные
        private async Task GetProxiesRequestV1()
        {
            LastUpdate = DateTime.UtcNow;
            using HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
            var res = await client.GetAsync($"https://cdn.jsdelivr.net/gh/proxifly/free-proxy-list@main/proxies/all/data.json");
            //                                https://cdn.jsdelivr.net/gh/proxifly/free-proxy-list@main/proxies/all/data.json
            if (res.IsSuccessStatusCode)
            {
                var json = await res.Content.ReadAsStringAsync();
                var list = JsonConvert.DeserializeObject<List<Proxifly>>(json);
                if (list != null && list.Count > 0)
                {
                    ProxysList.Clear();//тут очень много прокси поэтому удаляю старые прокси
                     list.Reverse();
                    //var protocols=list.GroupBy(p => p.protocol);
                    var anonymitys = list.Where(p => p.anonymity!= "transparent").OrderBy(p=>p.score).ToList();//берем только элитные прокси 
                   
                    anonymitys.ForEach(p => ProxysList.Push(new ProxyElement(p.ip, p.protocol, p.proxy)));
              

                    //foreach (var item in protocols)
                    //{
                    //    if (item.Key=="http")
                    //    {
                    //       var test= item.ToArray();
                    //        foreach (var p in test)
                    //        {
                    //            ProxysList.Push(new ProxyElement(p.ip, p.protocol, p.proxy));
                    //        }
                    //        // .ForEach(p => ProxysList.Push(new ProxyElement(p.ip, p.protocol, p.proxy)));
                    //    }

                    //}
                }
            }
        }
      
    }
}
