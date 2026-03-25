namespace WebAppCellMapper.Services
{
    public interface IProxyHandlerPoolService
    {
        HttpClientHandler? GetClientHandler();
        void ReleaseHandler(HttpClientHandler handler);
        void RemoveUnusedProxy();
    }

}
