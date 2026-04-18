using Microsoft.EntityFrameworkCore;
using WebAppLocator.Data.Models;
using WebAppLocator.Data.Statics;
using WebAppLocator.DTO;
using WebAppLocator.DTO.Cells;
using WebAppLocator.Service;

namespace WebAppLocator.Data.Repository
{
    public class LocationRepository: ILocationRepository
    {
        private readonly WorkflowService workflow;
        private readonly LocatorDbContext context;

        public LocationRepository(WorkflowService workflow,LocatorDbContext dbContext)
        {
            this.workflow = workflow;
            context = dbContext;
        }
        public async Task SaveLocation(string deviceId,LocationResponse response, DateTime? difTime = null)
        {
            var trace = new TracePoints()
            {
                DeviceId = deviceId,
                Lat = response.point.lat,
                Lon = response.point.lon,
                Accuracy = response.accuracy,
              
            };
            if (difTime.HasValue)
            {
                trace.Timestamp = difTime.Value;


            }
            await context.traces.AddAsync(trace);
            await context.SaveChangesAsync();
        }

        public async Task<LocationCell?> GetLastLocation(string deviceId,DateTime? difTime = null, CancellationToken ct=default)
        {

            var lastLoc = await context.traces
            .AsNoTracking()
            .Where(t => t.DeleteAt == null&&t.DeviceId == deviceId)
            .OrderByDescending(t => t.Timestamp)
            .FirstOrDefaultAsync();
            if (lastLoc == null) return null;

            var model = MapToCell(lastLoc, difTime);
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

        private LocationCell MapToCell(TracePoints lastLoc, DateTime? difTime=null)
        {

            var N = GeoHelper.N; // Коэффициент затухания (город) 2.0
            double A = GeoHelper.A;// RSSI на 1 метре 35
            if (!difTime.HasValue) difTime = DateTime.UtcNow;

            double secondsDiff = (difTime.Value - lastLoc.Timestamp).TotalSeconds;
            int dbmLastPos = -(60 + (int)(secondsDiff / 2.5));
            var model = new LocationCell(lastLoc.Id, lastLoc.Lat, lastLoc.Lon);
            model.SignalStrength = dbmLastPos;
            //model.WeightSignal = Math.Pow(10, (dbmLastPos / 20));
            model.DistanceSignal = Math.Pow(10, (A - dbmLastPos) / (10 * N));
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
           await context.traces.Where(t => t.Timestamp + TimeSpan.FromHours(2) < DateTime.UtcNow && t.DeleteAt == null)
                .ExecuteUpdateAsync(s=>s.SetProperty(t=>t.DeleteAt,DateTime.UtcNow),ct);
           return await context.traces.Where(t => t.DeleteAt != null&&t.DeleteAt + TimeSpan.FromDays(1) < DateTime.UtcNow).ExecuteDeleteAsync(ct);
        }
    }
}
