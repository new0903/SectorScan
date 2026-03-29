using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Net;
using WebAppCellMapper.DTO;
using WebAppCellMapper.Options;

namespace WebAppCellMapper.Proxy
{

    /*
     
     план

    создать Новый класс который будет хранить HttpClientHandler, время последнего запроса, id

    создать метод который будет обнулять ConcurrentDictionary 
    
     */


    public class ProxyHandlerPoolService: IProxyHandlerPoolService
    {
        private readonly ConcurrentStack<ProxyHandler> handlers;
        private readonly List<ProxyHandler> listHandlers;


        private readonly RequestSettings settings;
        private readonly IProxyService proxyService;
        private readonly ILogger<ProxyHandlerPoolService> logger;

        //public int countHandlers { get; private set; }

        public ProxyHandlerPoolService(IOptions<RequestSettings> options, IProxyService proxyService,ILogger<ProxyHandlerPoolService> logger)
        {
            handlers = new ConcurrentStack<ProxyHandler>();
            listHandlers = new List<ProxyHandler>();
            settings = options.Value;
            this.proxyService = proxyService;
            this.logger = logger;
        }

        public int CountProxy => proxyService.CountProxy + listHandlers.Count;//берем все количество(используемые и не используемые пока что)
        public int CountHandlers => listHandlers.Count;//Все прокси которые используются
        public int FreeCountHandlers => handlers.Count;//свободные прокси ожидающие запроса


        public async Task InitProxies()
        {
            await proxyService.GetProxies();
        }

        public ProxyHandler? GetClientHandler()
        {
            ProxyHandler? proxyHandler = null;
            try
            {
                //await proxyService.GetProxies();
                if (listHandlers.Count < settings.MaxConnectionsPerServer && proxyService.CountProxy>0)
                {
                   
                    //countHandlers++;
                    var proxy=proxyService.GetProxy();
                    if (proxy == null) return null;
                    //HttpClientHandler? httpHandler = null;
                    HttpClientHandler httpHandler = new HttpClientHandler();
                    proxyHandler = new ProxyHandler(httpHandler, UserAgents[Random.Shared.Next(UserAgents.Length)]);
                    httpHandler.Proxy = new WebProxy(proxy.url);
                    httpHandler.UseProxy = true;

                    httpHandler.AutomaticDecompression =  DecompressionMethods.All;//DecompressionMethods.GZip | DecompressionMethods.Deflate | DecompressionMethods.Brotli |

                    // Если нужно игнорировать сертификаты (только для тестирования)
                    //handler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;
                    listHandlers.Add(proxyHandler);
                  //  handlers.Enqueue(handler);

                }
                else
                {
                    handlers.TryPop(out proxyHandler);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
            }
            return proxyHandler;
        }

        public ProxyHandler CreateDefaultClient()
        {
            var handler = new HttpClientHandler();
            handler.AutomaticDecompression = DecompressionMethods.All;
            var ph = new ProxyHandler(handler, "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36");
            return ph;
        }
        /*
         хотя такая реализация не очень хорошая так скажем. Т.к. надо 100% отслеживать каждый handler.
        Надо будет подумать. 
         */
        public void RemoveUnusedProxy()
        {
            try
            {
                listHandlers.RemoveAll(item =>
                {
                    if (!handlers.Contains(item))
                    {
                        item.ClientHandler.Dispose();
                        return true;
                    }
                    return false;
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
            }

        }
        public void ReleaseHandler(ProxyHandler handler)
        {
            try
            {
                handlers.Push(handler);
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
            }

        }




        private readonly string[] UserAgents = new string[]
        {
              "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36",
              "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/121.0.0.0 Safari/537.36 Edg/121.0.0.0",
              "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:123.0) Gecko/20100101 Firefox/123.0",
              "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36",
              "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.0 Safari/605.1.15",
              "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/121.0.0.0 Safari/537.36 Edg/121.0.0.0",
              "Mozilla/5.0 (iPhone; CPU iPhone OS 17_4 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.0 Mobile/15E148 Safari/604.1",
              "Mozilla/5.0 (iPad; CPU OS 17_4 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.0 Mobile/15E148 Safari/604.1",
              "Mozilla/5.0 (Android 14; Mobile; rv:123.0) Gecko/123.0 Firefox/123.0",
              "Mozilla/5.0 (Android 14; Mobile; LG-M700) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Mobile Safari/537.36",
              "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/121.0.0.0 Safari/537.36 OPR/98.0.0.0",
              "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 YaBrowser/24.2.0.0 Safari/537.36",
              "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36 Vivaldi/6.5.0.0",
              "Mozilla/5.0 (Windows NT 10.0; WOW64; Trident/7.0; rv:11.0) like Gecko",
              "Mozilla/5.0 (Windows NT 6.1; Win64; x64; rv:109.0) Gecko/20100101 Firefox/115.0",
              "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/109.0.0.0 Safari/537.36",
              "Mozilla/5.0 (Windows NT 6.3; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36",
              "Mozilla/5.0 (iPhone; CPU iPhone OS 16_7 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/16.0 Mobile/15E148 Safari/604.1",
              "Mozilla/5.0 (iPad; CPU OS 16_7 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/16.0 Mobile/15E148 Safari/604.1",
              "Mozilla/5.0 (Linux; Android 12; SM-G973F) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/121.0.0.0 Mobile Safari/537.36",
              "Mozilla/5.0 (Linux; Android 11; moto g(9) play) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Mobile Safari/537.36",
              "Mozilla/5.0 (Linux; Android 10; K) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/121.0.0.0 Mobile Safari/537.36",
              "Mozilla/5.0 (Linux; Android 9; SM-J530F) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/109.0.0.0 Mobile Safari/537.36",
              "Mozilla/5.0 (Linux; Android 8.0.0; SM-G960F) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/109.0.0.0 Mobile Safari/537.36",
              "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/121.0.0.0 Safari/537.36 Electron/28.0.0",
              "Mozilla/5.0 (Linux; Android 6.0.1; Nexus 5X Build/MMB29P) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Mobile Safari/537.36 (compatible; Googlebot-Mobile/2.1; +http://www.google.com/bot.html)",
              "Mozilla/5.0 (iPhone; CPU iPhone OS 17_4 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) CriOS/122.0.0.0 Mobile/15E148 Safari/604.1",
              "Mozilla/5.0 (iPad; CPU OS 17_4 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) CriOS/122.0.0.0 Mobile/15E148 Safari/604.1",
              "Mozilla/5.0 (Windows NT 10.0; Win64; x64; Xbox; Xbox One) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/121.0.0.0 Safari/537.36 Edge/44.18363.8131",
             
        };

    }
}
