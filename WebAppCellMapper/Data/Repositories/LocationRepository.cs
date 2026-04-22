using Microsoft.EntityFrameworkCore;
using WebAppCellMapper.Data;
using WebAppCellMapper.Data.Models;
using WebAppCellMapper.DTO.Locator;
using WebAppCellMapper.DTO.Locator.Cells;
using WebAppCellMapper.Helpers;
using WebAppCellMapper.Services;


namespace WebAppCellMapper.Data.Repositories
{
    public class LocationRepository: ILocationRepository
    {
        private readonly WorkflowService workflow;
        private readonly AppDBContext context;

        public LocationRepository(WorkflowService workflow,AppDBContext dbContext)
        {
            this.workflow = workflow;
            context = dbContext;
        }
        public async Task SaveLocation(string deviceId,LocationResponse response, DateTime difTime )
        {
            if (string.IsNullOrEmpty(deviceId)) return;
            var trace = new TracePoints()
            {
                DeviceId = deviceId,
                Lat = response.point.lat,
                Lon = response.point.lon,
                Accuracy = response.accuracy,
              Timestamp=difTime
            };

            await context.traces.AddAsync(trace);
            await context.SaveChangesAsync();
        }

        public async Task<LocationCell?> GetLastLocation(string deviceId,DateTime difTime, CancellationToken ct=default)
        {
            if (string.IsNullOrEmpty(deviceId)) return null;

            var lastLoc = await context.traces
            .AsNoTracking()
            .Where(t => t.DeleteAt == null&&t.DeviceId == deviceId)
            .OrderByDescending(t => t.Timestamp)
            .FirstOrDefaultAsync();
            if (lastLoc == null) return null;

            if ((difTime-lastLoc.Timestamp).TotalMinutes > 30) return null;//если точке более 30 минут тогда игнорируем её и делаем расчеты только на бс
            LocationCell model = MapToCell(lastLoc, difTime);
            if (model.SignalStrength>600) return null;//если с связью проблемы, сигнал меньше 600 и меньше 30 позиции минут можно вычислить азимут откуда мы едем и получить приблизительную позицию в поле
            return model;
        }


        public async Task<List<LocationPoint>> GetListLastLocation(string deviceId, CancellationToken ct)
        {
            var list= await context.traces
            .AsNoTracking()
            .Where(t => t.DeviceId == deviceId && t.DeleteAt==null)
            .OrderByDescending(t => t.Timestamp)
            .Select(t=>new LocationPoint(t.Lat,t.Lon))
            .Take(10)
            .ToListAsync(ct);

            return list;//list.Select(t => MapToCell(t)).ToList();
        }

        private LocationCell MapToCell(TracePoints lastLoc, DateTime difTime)
        {

            var N = GeoHelper.N;
            double A = GeoHelper.A;
            double LPS = GeoHelper.LPS;
           // if (!difTime.HasValue) difTime = DateTime.UtcNow;

            double secondsDiff = (difTime - lastLoc.Timestamp).TotalSeconds;
            int dbmLastPos = -(int)(LPS + (secondsDiff / 2.5));
            var model = new LocationCell(lastLoc.Id, lastLoc.Lat, lastLoc.Lon);
            model.SignalStrength = dbmLastPos;
            model.DistanceSignal = Math.Pow(10, (A - dbmLastPos) / (10 * N));
            model.Timestamp = lastLoc.Timestamp;
            return model;
        }

        public async Task<List<LocationCell>> GetStationsLocation(long[] ids, CancellationToken ct)
        {
            var stations = await context.stations.AsNoTracking()
            .Where(s => ids.Contains(s.Id))
            .Select(s => new LocationCell(s.Id, s.Lat, s.Lon))
            .ToListAsync();
            return stations;
        }

        public async Task<int> EraseOldLocation(CancellationToken ct)
        {
           await context.traces.Where(t => t.Timestamp + TimeSpan.FromMinutes(40) < DateTime.UtcNow && t.DeleteAt == null)
                .ExecuteUpdateAsync(s=>s.SetProperty(t=>t.DeleteAt,DateTime.UtcNow),ct);//каждые 2 часа помечаем на удаление старые записи
           return await context.traces.Where(t => t.DeleteAt != null&&t.DeleteAt + TimeSpan.FromDays(1) < DateTime.UtcNow).ExecuteDeleteAsync(ct); // через 24 часа удаляем
        }
    }
}
