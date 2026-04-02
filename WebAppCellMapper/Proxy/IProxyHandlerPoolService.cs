using WebAppCellMapper.DTO;

namespace WebAppCellMapper.Proxy
{
    public interface IProxyHandlerPoolService
    {
        int CountHandlers { get; }
        int CountProxy { get; }
        int FreeCountHandlers {  get; }
        Task InitProxies();
        ProxyHandler? GetClientHandler();
        void ReleaseHandler(ProxyHandler handler);
        void RemoveUnusedProxy();
        string GetUserAgent();
    }

}
