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
        private readonly GeoHelper geoHelper;
        private readonly ILogger<LocatorService> logger;

        // private readonly LocatorDbContext context;

        public LocatorService(
            ILocationRepository repository,
            IGraphHopperService hopperService,
            GeoHelper geoHelper,
            ILogger<LocatorService> logger)
        {
            this.repository = repository;
            this.hopperService = hopperService;
            this.geoHelper = geoHelper;
            this.logger = logger;
            //this.context = context;
        }
       
       
     /// <summary>
     /// вычисляю центроид
     /// </summary>
     /// <param name="stations"> станции </param>
     /// <returns></returns>
        public LocationResponse FindLocationDefault(List<LocationCell> stations)
        {

            double cx = 0, cy = 0, sw = 0;
            double weightedSumSq = 0;


            foreach (var item in stations)
            {
                double testweight = 1 / (item.DistanceSignal); //(item.DistanceSignal);
                cy += item.Lat * testweight;
                cx += item.Lon * testweight;
                sw += testweight;


            }
            double centroidLon = cx / sw;
            double centroidLat = cy / sw;
            foreach (var item in stations)
            {

                double acc = 1 / (item.DistanceSignal);
                double diffLat = item.Lat - centroidLat;
                double diffLon = item.Lon - centroidLon;
                double distanceSq = diffLat * diffLat + diffLon * diffLon;
                weightedSumSq += acc * distanceSq;

            }

            double accuracy = geoHelper.DistancePerMeters(Math.Sqrt(weightedSumSq / sw));
            var res = new LocationResponse(new LocationPoint(centroidLat, centroidLon), accuracy, "метод центроида");
            return res;
        }


       
      
        /// <summary>
        /// это для переноса точки на дорогу опционально
        /// </summary>
        /// <param name="centroid">центроид</param>
        /// <param name="deviceId">для получения старой позиции</param>
        /// <returns></returns>
        public async Task<LocationResponse?> FindLocationDefaultWithGraph(LocationResponse centroid, string deviceId)
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
                var point= result.First();
                double lon = point.lon;
                double lat = point.lat;
                var res = new LocationResponse(new LocationPoint(lat, lon), centroid.accuracy, "метод центроида + graph");
                return res;
            }
            return null;
        }


        /// <summary>
        /// не дает улететь в космос. Помогает за городом. Ограничивает максимально возможную дистанцию 
        /// </summary>
        /// <param name="centroid">позиция центроида</param>
        /// <param name="lastLocation">последняя позиция</param>
        /// <param name="difTime">текущее время</param>
        /// <returns></returns>
        public LocationResponse? CheckDistance(LocationPoint centroid, LocationCell lastLocation, DateTime difTime)
        {
            var distance= geoHelper.DistancePerMeters(centroid.lat, centroid.lon,lastLocation.Lat,lastLocation.Lon);
            var secondDif = (difTime - lastLocation.Timestamp).TotalSeconds;

            var kmPerH = (distance / secondDif)* 3.6;
            //очевидно что нельзя с такой скорост перемещаться поэтому предпологаем что это ложь и указываем нормальную дистанцию
            if (kmPerH > 120) 
            {
                var destination = geoHelper.GetBearingDegrees(centroid.lat, centroid.lon, lastLocation.Lat, lastLocation.Lon);
                var maxDistance = 33 * secondDif;//37 м/с 23333 Метров/минута 140 Км/ч



                var difLoc= geoHelper.OffsetByMeters(centroid.lat, centroid.lon, distance- maxDistance, destination);
                return new LocationResponse(difLoc,0,$"центроид + dif локации на {distance-maxDistance} метров");
            }
            return null;
        }

        /// <summary>
        /// вычисляем позицию
        /// </summary>
        /// <param name="request">тело запроса</param>
        /// <param name="deviceId">Нужно для нахождения старых позиций</param>
        /// <returns></returns>
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
                        var g = group?.First(c => c.Key == item.Id).OrderByDescending(c => c?.SignalStrength);//.First();
                        var tg = g.First();
                        if (tg == null) continue;
                        //item.WeightSignal = tg.WeightSignal;
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

            if (ids.Contains(250001660256))
            {
                Console.WriteLine();
            }
            logger.LogInformation($"count points: {stations.Count}");
            var centroid = FindLocationDefault(stations);
            res.Add(centroid);

            if (lastLoc!=null)
            {
                var difPos = CheckDistance(centroid.point, lastLoc, request.Timestamp);
                if (difPos != null)
                {
                    res.Add(difPos);
                    centroid=difPos;
                }
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
