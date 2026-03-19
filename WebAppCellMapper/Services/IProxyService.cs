namespace WebAppCellMapper.Services
{
    public interface IProxyService
    {
         Task GetProxies();
         ProxyElement? GetProxy();
         void ReleaseProxy(ProxyElement proxy);
    }
}
