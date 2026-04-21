

using WebAppCellMapper.DTO.Locator;

namespace WebAppCellMapper.Services
{
    public interface ILocatorService
    {
        Task<LocationResponse?> FindLocation(LocationRequest request, string deviceId);
    }
}
