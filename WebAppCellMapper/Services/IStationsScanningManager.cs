using WebAppCellMapper.DTO;

namespace WebAppCellMapper.Services
{
    public interface IStationsScanningManager
    {
        void StartFullScan();
        QueryResult GetCurrentProcess();
        Task StopCurrentProccess();
        Task<int> CanceledProccess();
    }
}
