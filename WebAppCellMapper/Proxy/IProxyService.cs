namespace WebAppCellMapper.Proxy
{
    public interface IProxyService
    {
         int CountProxy {  get; }
         Task GetProxies();
         ProxyElement? GetProxy();
         void ReleaseProxy(ProxyElement proxy);
    }
}
