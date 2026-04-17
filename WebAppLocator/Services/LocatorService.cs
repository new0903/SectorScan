using Domain.Models;
using Google.Protobuf.WellKnownTypes;
using MathNet.Numerics.LinearAlgebra.Double;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using WebAppLocator.Data;
using WebAppLocator.Data.Models;
using WebAppLocator.Data.Repository;
using WebAppLocator.Data.Statics;
using WebAppLocator.DTO;
using WebAppLocator.DTO.Cells;
using WebAppLocator.Helpers;
using WebAppLocator.Service;


namespace WebAppCellMapper.Services
{

  //  public record LocationCell(long id,double lat,double lon);


    public class LocatorService : ILocatorService
    {
        private readonly ILocationRepository repository;
        private readonly IGraphHopperService hopperService;
        private readonly ILogger<LocatorService> logger;

        // private readonly LocatorDbContext context;

        public LocatorService(
            ILocationRepository repository,
            IGraphHopperService hopperService,
            ILogger<LocatorService> logger)
        {
            this.repository = repository;
            this.hopperService = hopperService;
            this.logger = logger;
            //this.context = context;
        }
       
       
     
        public LocationResponse FindLocationDefault(List<LocationCell> stations)
        {

            double cx = 0, cy = 0, sw = 0;
            // Второй проход — вычисляем взвешенную точность
            double weightedSumSq = 0;


            foreach (var item in stations)
            {
                double testweight = 1 / item.DistanceSignal; //(item.DistanceSignal);
                cy += item.Lat * testweight;
                cx += item.Lon * testweight;
                sw += testweight;


            }
            double centroidLon = cx / sw;
            double centroidLat = cy / sw;
            foreach (var item in stations)
            {

                double acc = 1.0 / item.DistanceSignal;
                // Евклидово расстояние между станцией и центроидом
                double diffLat = item.Lat - centroidLat;
                double diffLon = item.Lon - centroidLon;
                double distanceSq = diffLat * diffLat + diffLon * diffLon;
                weightedSumSq += acc * distanceSq;

            }

            // Взвешенное стандартное отклонение (корень из взвешенной дисперсии)
            double accuracy = Math.Sqrt(weightedSumSq / sw);
            var res = new LocationResponse(new LocationPoint(centroidLat, centroidLon), accuracy, "метод центроида");
            return res;
        }


      

        public async Task<LocationResponse?> FindLocationDefaultWithGraph(LocationResponse centroid, string deviceId)//, string deviceId
        {
            List<LocationPoint> list=new List<LocationPoint>();
            var tracks= await repository.GetListLastLocation(deviceId);
            if (tracks.Count>0)
            {
                list.Add(tracks.First());
            }
            list.Add(centroid.point);
            if (list == null||list.Count<2) return null;
            List<LocationPoint> result =await hopperService.MatchRoadAsync(list);
            if (result!=null&& result.Count>0)
            {
                //var path=result.Paths.FirstOrDefault();
                //if (path == null) return null;
                //int counter = (int)(path.Points.Coordinates.Length * 0.94);

                //var coordinates = path.Points.Coordinates[counter];//.FirstOrDefault();
                //if (coordinates == null) return null;
                int counter = (int)(result.Count * 0.9);
                double lon = result[counter].lon;
                double lat = result[counter].lat;
                var res = new LocationResponse(new LocationPoint(lat, lon), centroid.accuracy, "метод центроида + graph");
                return res;
            }
            return null;
        }

        public async Task<List<LocationResponse>> FindLocation(LocationRequest request, string deviceId)
        {

            var res = new List<LocationResponse>();
            if (string.IsNullOrEmpty(deviceId)) return res;
            var cells= request?.Cell?.Select(c => c.Data).Where(c => c != null).ToArray();
            var ids = cells?.Select(c => c.Id).Distinct().ToArray();

            var stations =await repository.GetStationsLocation(ids);
            if (stations.Count == 0) return res;
            {
                var group = cells?.GroupBy(c => c.Id).ToArray();

                foreach (var item in stations)
                {

                    try
                    {
                        var g = group?.First(c => c.Key == item.Id).OrderByDescending(c => c?.WeightSignal);//.First();
                        var tg = g.First();
                        if (tg == null) continue;
                        item.WeightSignal = tg.WeightSignal;
                        item.SignalStrength = tg.SignalStrength;
                        item.DistanceSignal = tg.DistanceSignal;
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex.Message, ex);
                    }

                }

            }
            var lastLoc=await repository.GetLastLocation(deviceId, request.Timestamp);
            if (lastLoc != null)
            {
                stations.Add(lastLoc);
            }
            else
            {
                logger.LogInformation($"last location null");
            }

            logger.LogInformation($"count points: {stations.Count}");
            var centroid = FindLocationDefault(stations);
            res.Add(centroid);
 

            if (lastLoc!=null)
            {
                var LG = await FindLocationDefaultWithGraph(centroid, deviceId);
                if (LG != null)
                {
                    res.Add(LG);
                    centroid = LG;
                }
            }
            await repository.SaveLocation(deviceId, centroid, request.Timestamp);




            return res;
        }
    }
}
