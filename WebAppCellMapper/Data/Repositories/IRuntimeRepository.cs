namespace WebAppCellMapper.Data.Repositories
{
    public interface IRuntimeRepository
    {
        Task StartRuntime();
        Task<bool> IsRunning();
        Task StopRuntime();
        Task CancelRuntime(CancellationToken ct=default);
    }
}
