using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Net;
using WebAppCellMapper.Options;

namespace WebAppCellMapper.Services
{

    /*
     
     план

    создать Новый класс который будет хранить HttpClientHandler, время последнего запроса, id

    создать метод который будет обнулять ConcurrentDictionary 
    
     */

    public class ProxyHandlerPoolService: IProxyHandlerPoolService
    {
        private readonly ConcurrentStack<HttpClientHandler> handlers;
        private readonly List<HttpClientHandler> listHandlers;


        private readonly RequestSettings settings;
        private readonly IProxyService proxyService;
        private readonly ILogger<ProxyHandlerPoolService> logger;

        //public int countHandlers { get; private set; }

        public ProxyHandlerPoolService(IOptions<RequestSettings> options, IProxyService proxyService,ILogger<ProxyHandlerPoolService> logger)
        {
            handlers = new ConcurrentStack<HttpClientHandler>();
            listHandlers = new List<HttpClientHandler>();
            settings = options.Value;
            this.proxyService = proxyService;
            this.logger = logger;
        }
        public async Task InitProxies()
        {
            await proxyService.GetProxies();
        }

        public HttpClientHandler? GetClientHandler()
        {
            HttpClientHandler? handler = null;
            try
            {
                //await proxyService.GetProxies();
                if (listHandlers.Count < settings.MaxConnectionsPerServer)
                {
                    
                    //countHandlers++;
                    var proxy=proxyService.GetProxy();
                    if (proxy == null) return null;
                    handler = new HttpClientHandler();
                    handler.Proxy = new WebProxy(proxy.url);
                    handler.UseProxy = true;
                    handler.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate | DecompressionMethods.Brotli;
                    // Если нужно игнорировать сертификаты (только для тестирования)
                    handler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;
                    listHandlers.Add(handler);
                  //  handlers.Enqueue(handler);

                }
                else
                {
                    handlers.TryPop(out handler);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
            }
            return handler;
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
                        item.Dispose();
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
        public void ReleaseHandler(HttpClientHandler handler)
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
    }
}
