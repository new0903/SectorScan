using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using System;
using System.Diagnostics;
using System.Linq;
using WebAppCellMapper.Data;
using WebAppCellMapper.Data.Models;
using WebAppCellMapper.DTO;
using WebAppCellMapper.Helpers;

namespace WebAppCellMapper.Data.Repositories
{
    public class ProgressRepository : IProgressRepository
    {
        private readonly AppDBContext context;
        private readonly ILogger<ProgressRepository> logger;

        public ProgressRepository(AppDBContext context,ILogger<ProgressRepository> logger)
        {
            this.context = context;
            this.logger = logger;
        }

        /*init progress*/
        public async Task InitProgress(CancellationToken ct = default)//OperatorDTO op, NetworkStandard ns,IEnumerable<SquareSearch>? squares,
        {

    


            var operators = await context.operators.AsNoTracking().ToListAsync(ct);


            foreach (var o in operators)
            {
                foreach (var network in NSEnumerator.GetNetwork)//тип сети
                {
                    var progress = new OperatorProgress()
                    {
                        CreatedAt = DateTime.UtcNow,
                        OperatorId = o.Id,
                        Status = ProgressStatus.Pending,
                        Standard = network,
                        AddedStationsCount = 0,
                        ScannedCount = 0,
                        TotalCount = 0,
                        Coordinates = new List<SquareSearch>()
                        //  Data = new ProgressData { AddedStationsCount = 0, Coordinates = new List<SquareSearch>(), ScannedCount = 0, TotalCount = 0}
                    };
                    await context.progresses.AddAsync(progress);
                }
            }
            await context.SaveChangesAsync(ct);
        }

        /*save progress*/
        public async Task<int> SaveProgress(OperatorDTO entity, CancellationToken ct = default)//OperatorDTO op, NetworkStandard ns,IEnumerable<SquareSearch>? squares,
        {
            try
            {
   
                return await context.progresses.Where(p => p.Id == entity.Id).
                    ExecuteUpdateAsync((s) => s
                    .SetProperty(o => o.Status, entity.Status)
                    .SetProperty(o => o.CompletedAt, entity.CompletedAt)
                    .SetProperty(o => o.UpdatedAt, DateTime.UtcNow)
                    .SetProperty(o => o.StartedAt, entity.StartedAt)
                    .SetProperty(o => o.Coordinates, entity.Coordinates)
                    .SetProperty(o => o.ScannedCount, entity.ScannedCount)
                    .SetProperty(o => o.AddedStationsCount, entity.AddedStationsCount)
                    .SetProperty(o => o.TotalCount, entity.TotalCount)
                    , ct);

                // return await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
            }
            return 0;

        }


        /*load progress*/
        public async Task<List<OperatorDTO>> LoadProgress( CancellationToken ct=default)//OperatorDTO op, NetworkStandard ns,IEnumerable<SquareSearch>? squares,
        {
            await DeleteCompletedProgress(ct);
            return await context.progresses
                .AsNoTracking()
                .Where(p => p.Status != ProgressStatus.Completed && p.Status!= ProgressStatus.Failed)
                .OrderBy(p=>p.Id)
                .Select(q =>
                    new OperatorDTO(
                        q.Id,
                        q.Operator.Id,
                        q.Operator.InternalCode,
                        q.Standard,
                        q.Status,
                        q.ScannedCount,
                        q.TotalCount,
                        q.AddedStationsCount,
                        q.Coordinates,
                        q.StartedAt,
                        q.CompletedAt
                    )
                )
                .ToListAsync(ct);
   
        }

        /*удаляем старое*/
        public async Task<int> DeleteCompletedProgress(CancellationToken ct = default)
        {
           return await context.progresses
                .Where(p=>p.Status==ProgressStatus.Completed&& p.Status==ProgressStatus.Failed&& DateTime.UtcNow>p.CompletedAt+TimeSpan.FromDays(15))//наверное так лучше
                .ExecuteDeleteAsync(ct);
        }

        public async Task<int> FailedProgress(CancellationToken ct = default)
        {
            return await context.progresses
                .Where(p => p.Status != ProgressStatus.Completed && p.Status != ProgressStatus.Failed)//наверное так лучше
                .ExecuteUpdateAsync(s=>s.SetProperty(p=>p.Status, ProgressStatus.Failed),ct);
        }



    }
}
