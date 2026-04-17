using WebAppCellMapper.Data.Models;
using WebAppCellMapper.DTO;

namespace WebAppCellMapper.Data.Repositories
{
    public interface IProgressRepository
    {
        Task InitProgress(CancellationToken ct = default);
        Task<int> SaveProgress(ProgressDTO progress, CancellationToken ct = default);
        Task<List<ProgressDTO>> LoadProgress(CancellationToken ct = default);
        Task<int> DeleteCompletedProgress(CancellationToken ct = default);
        Task<int> FailedProgress(CancellationToken ct = default);
    }
}
