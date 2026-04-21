

using WebAppCellMapper.DTO.Locator;

namespace WebAppCellMapper.Services
{
    public interface ILocatorService
    {
        Task<List<LocationResponse>> FindLocation(LocationRequest request, string deviceId);
    }
}
