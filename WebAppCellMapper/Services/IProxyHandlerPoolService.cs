namespace WebAppCellMapper.Services
{
    public interface IProxyHandlerPoolService
    {
        Task InitProxies();
        HttpClientHandler? GetClientHandler();
        void ReleaseHandler(HttpClientHandler handler);
        void RemoveUnusedProxy();
    }

}
