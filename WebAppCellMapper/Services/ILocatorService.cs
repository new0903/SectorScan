

using WebAppCellMapper.DTO.Locator;

namespace WebAppCellMapper.Services
{
    public interface ILocatorService
    {
        Task<LocationAnswer?> FindLocation(LocationRequest request, string deviceId);
    }
}
