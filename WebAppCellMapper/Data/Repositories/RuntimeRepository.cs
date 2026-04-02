using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WebAppCellMapper.Data.Models;

namespace WebAppCellMapper.Data.Repositories
{
    public class RuntimeRepository : IRuntimeRepository
    {
        private readonly AppDBContext context;
        private readonly ILogger<RuntimeRepository> logger;

        public RuntimeRepository(AppDBContext context, ILogger<RuntimeRepository> logger)
        {
            this.context = context;
            this.logger = logger;
        }
        public async Task<bool> IsRunning()
        {
            //Проверка на случай если надо авто старт сделать и т.д.
            return await context.runtime.AnyAsync(r => r.IsProcessing);
        }

        public async Task StartRuntime()
        {



            var isRunning = await context.runtime.CountAsync(p => p.IsProcessing);

            //если что то там запущено
            if (isRunning > 0)
                return;


            var resumed = await context.runtime
            .Where(p => !p.IsProcessing && p.FinishedAt == null)
            .ExecuteUpdateAsync(s => s
                .SetProperty(p => p.IsProcessing, true));

            //если что то там поменяли
            if (resumed > 0)
                return;

            var runtime = new AppRuntimeState();
            runtime.IsProcessing = true;
            await context.runtime.AddAsync(runtime);
            await context.SaveChangesAsync();





        }
        public async Task StopRuntime()
        {
            //остановили процесс, можно возобновить
            await context.runtime.Where(r => r.IsProcessing)
                .ExecuteUpdateAsync(s => s
                .SetProperty(p => p.IsProcessing, false));

        }

        public async Task CancelRuntime(CancellationToken ct)
        {
            try
            {

                //завершили процесс
                await context.runtime.Where(r => r.FinishedAt == null)
                    .ExecuteUpdateAsync(s => s
                    .SetProperty(p => p.FinishedAt, DateTime.UtcNow)
                    .SetProperty(p => p.IsProcessing, false), ct);
            }
            catch (OperationCanceledException)
            {
                logger.LogInformation("CancelRuntime");
            }

        }
    }
}
