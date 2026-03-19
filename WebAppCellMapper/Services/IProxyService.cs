namespace WebAppCellMapper.Services
{
    public interface IProxyService
    {
        public  Task<IReadOnlyCollection<ProxyElement>> GetProxies();
        public Task<ProxyElement?> GetProxy();
        public void ReleaseProxy(ProxyElement proxy);
    }
}
