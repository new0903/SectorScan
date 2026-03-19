
using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.Collections.Generic;
//using HtmlAgilityPack;


namespace WebAppCellMapper.Services
{
    public record Geolocation(string country,string city);
    public record Proxifly(string proxy,string protocol,string ip,int port,bool https,string anonymity,int score,Geolocation geolocation);
   
    
    //public record ProxyElement
    //{
    //    public string Ip { get; init; }
    //    public string Protocol { get; init; }
    //    public string Url { get; init; }
    //    public bool IsBusy { get; set; } 

    //    public ProxyElement(string ip, string protocol, string url, bool isBusy = false)
    //    {
    //        Ip = ip;
    //        Protocol = protocol;
    //        Url = url;
    //        IsBusy = isBusy;
    //    }

    //    //// Вычисляемое свойство для удобства
    //    //public string FullAddress => $"{Protocol}://{Url}";
    //}

    public record ProxyElement(string ip, string protocol,string url);
    public class ProxyService : IProxyService
    {
        private readonly ILogger<ProxyService> logger;

        //ещё надо будет подумать над  свободными и работающими прокси 
        //типа если прокси выполняет запрос то не трогаем его
        //но это на потом если надо будет
        
        private ConcurrentStack<ProxyElement> ProxysList { get; set; }
        public DateTime LastUpdate {  get;private set; }
        public ProxyService(ILogger<ProxyService> logger) {
            ProxysList=new ConcurrentStack<ProxyElement>();
           // requestUpdateProxy = false;
            //Task.Run(GetProxiesRequest);
            this.logger = logger;
        }

       

        //IEnumerable<ProxyElement> 
        public async Task<IReadOnlyCollection<ProxyElement>> GetProxies()
        {
            if (LastUpdate+TimeSpan.FromMinutes(60)<DateTime.UtcNow|| ProxysList.Count<10)//минимум 10 проксей
            {
                await GetProxiesRequest();//надо подумать про бд
                //await GetProxiesRequest();// старые прокси может пригодятся
            }
            return ProxysList.Take(150).ToArray();//это дело надо обдумать
            //разделить бы на занятые типа isBusy и не занятые и по категориям может потом разделить

        }



        public async Task< ProxyElement?> GetProxy()
        {
            try
            { 
                if (LastUpdate + TimeSpan.FromMinutes(60) < DateTime.UtcNow || ProxysList.Count < 10)//минимум 10 проксей
                {
                    await GetProxiesRequest();
                    //requestUpdateProxy = true;
                }
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

  
        private async Task GetProxiesRequest()
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
                     list.Reverse();

                    list.ForEach(p => ProxysList.Push(new ProxyElement(p.ip, p.protocol, p.proxy)));
                }
            }
        }
      
    }
}
