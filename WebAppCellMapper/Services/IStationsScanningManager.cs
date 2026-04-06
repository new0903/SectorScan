using System.Threading.Tasks;
using WebAppCellMapper.DTO;

namespace WebAppCellMapper.Services
{
    public interface IStationsScanningManager
    {
        void StartFullScan(bool isAutoStart=false);
       // void StartFullScan();
       // void AutoStartFullScan();
        QueryResult GetCurrentProcess {  get; }
        bool IsWorking { get; }
       
        Task StopCurrentProccess();
        Task<int> CanceledProccess();
        Task<List<QueryResult>> GetStats();
    }
}
