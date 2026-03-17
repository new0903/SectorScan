using HtmlAgilityPack;
using Newtonsoft.Json;

namespace WebAppCellMapper.Services
{
    public record Geolocation(string country,string city);
    public record Proxifly(string proxy,string protocol,string ip,int port,bool https,string anonymity,int score,Geolocation geolocation);


    public record ProxyElement(string ip, string protocol,string url);
    public class ProxyService
    {
        //ещё надо будет подумать над  свободными и работающими прокси 
        //типа если прокси выполняет запрос то не трогаем его
        //но это на потом если надо будет
        private List<ProxyElement> ProxysList { get; set; }
        public DateTime LastUpdate {  get;private set; }
        public ProxyService() {
            ProxysList=new List<ProxyElement>();
        }

        private readonly object obj=new object();

        //IEnumerable<ProxyElement> 
        public async Task<IReadOnlyCollection<ProxyElement>> GetProxies()
        {
            if (LastUpdate+TimeSpan.FromMinutes(60)<DateTime.UtcNow|| ProxysList.Count<1)
            {
                await GetProxiesRequestV1();//надо подумать про бд
                //await GetProxiesRequest();// старые прокси может пригодятся
            }
            return ProxysList.ToList().AsReadOnly();//это дело надо обдумать 
            //копии копиями но наверное стоит написать метод на подобии firstOrDefault, который возвращает не используемые прокси.
        }

        public void DeleteProxy(string address)
        {
            lock (obj)
            {
                var proxy = ProxysList.FirstOrDefault(p => p.url == address);
                if (proxy != null)
                {

                    ProxysList.Remove(proxy);
                }

            }

        }

        public async Task GetProxiesRequest()
        {
            LastUpdate=DateTime.UtcNow;
            using HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");

            int i = 1;
            while (true) 
            {
                var res = await client.GetAsync($"https://proxymania.su/free-proxy?page={i}");//https://proxymania.su/free-proxy?type=&country=RU&speed=
                if (res.IsSuccessStatusCode)
                {
                    var html = await res.Content.ReadAsStringAsync();
                    var list = ParseProxyTable(html);
                    if (list != null&& list.Count>0)
                    {
                        ProxysList.AddRange(list);

                        i++;
                    }
                    else
                    {
                        break;
                    }
                }
            }
           

        }
        public async Task GetProxiesRequestV1()
        {
            LastUpdate = DateTime.UtcNow;
            using HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
            var res = await client.GetAsync($"https://cdn.jsdelivr.net/gh/proxifly/free-proxy-list@main/proxies/all/data.json");
            if (res.IsSuccessStatusCode)
            {
                var json = await res.Content.ReadAsStringAsync();
                var list = JsonConvert.DeserializeObject<List<Proxifly>>(json);
                if (list != null && list.Count > 0)
                {
                    ProxysList.Clear();//тут очень много прокси поэтому удаляю старые прокси
                    ProxysList.AddRange(list.Select(p=>new ProxyElement(p.ip,p.protocol,p.proxy)));

                }
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
                        if (!string.IsNullOrEmpty(type) &&
                            (type.Contains("HTTP") || type.Contains("SOCKS")))
                        {
                            proxies.Add(new ProxyElement(ipProxy, type,$"{type.ToLower()}://{ipProxy}"));
                        }
                    }
                }
            }

            return proxies;
        }
    }
}
