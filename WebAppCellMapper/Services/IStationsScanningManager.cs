using WebAppCellMapper.DTO;

namespace WebAppCellMapper.Services
{
    public interface IStationsScanningManager
    {
        void StartFullScan();
        QueryResult GetCurrentProccess();
        Task StopCurrentProccess();
        Task<int> CanceledProccess();
    }
}
