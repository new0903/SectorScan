using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Net;
using WebAppCellMapper.Options;

namespace WebAppCellMapper.Services
{

    /*
     
     план

    создать Новый класс который будет хранить HttpClientHandler, время последнего запроса, id
    создать коллекцию private readonly ConcurrentDictionary которая будет хранить используемые прокси

    создать метод который будет обнулять ConcurrentDictionary 
    
     */

    public class ProxyHandlerPoolService: IProxyHandlerPoolService
    {
        private readonly ConcurrentStack<HttpClientHandler> handlers;
        

        private readonly RequestSettings settings;
        private readonly IProxyService proxyService;
        private readonly ILogger<ProxyHandlerPoolService> logger;

        public int countHandlers { get; private set; }

        public ProxyHandlerPoolService(IOptions<RequestSettings> options, IProxyService proxyService,ILogger<ProxyHandlerPoolService> logger)
        {
            handlers = new ConcurrentStack<HttpClientHandler>();
            settings= options.Value;
            countHandlers = 0;
            this.proxyService = proxyService;
            this.logger = logger;
        }


        public HttpClientHandler? GetClientHandler()
        {
            HttpClientHandler? handler = null;
            try
            {
                if (countHandlers < settings.MaxConnectionsPerServer)
                {
                    countHandlers++;
                    var proxy=proxyService.GetProxy();
                    if (proxy == null) return null;
                    handler = new HttpClientHandler();
                    handler.Proxy = new WebProxy(proxy.url);
                    handler.UseProxy = true;
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
        Надо будет подумать над автоматизацией этого дела. 
        Написать свой класс что ли который будет наследовать IDispose интерфейс и в деструкторе прописывать явный dispose хз вообщем.
        А может создать ещё коллекцию и метод которая будет вычитать только те элементы которые не вернулись. 
         */
        public void RemoveProxy(HttpClientHandler handler)
        {
            try
            {
                handler.Dispose();
                countHandlers--;
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
