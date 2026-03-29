using WebAppCellMapper.Data.Models;
using WebAppCellMapper.DTO;

namespace WebAppCellMapper.Data.Repositories
{
    public interface IProgressRepository
    {
        Task InitProgress(CancellationToken ct = default);
        Task<int> SaveProgress(OperatorDTO progress, CancellationToken ct = default);
        Task<List<OperatorDTO>> LoadProgress(CancellationToken ct = default);
        Task<int> DeleteCompletedProgress(CancellationToken ct = default);
        Task<int> FailedProgress(CancellationToken ct = default);
    }
}
