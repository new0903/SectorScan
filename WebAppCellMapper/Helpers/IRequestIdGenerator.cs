using WebAppCellMapper.DTO;
using WebAppCellMapper.Services;

namespace WebAppCellMapper.Helpers
{
    public interface IRequestIdGenerator
    {
        string GenerateRequestId(string path, string lastId = "", string userAgent = null);
        Task<bool> InitRequest(ProxyHandler handler, CancellationToken ct=default);
    }
}
