using System.Collections.Concurrent;

namespace WebAppCellMapper.Services
{
    public record SquareSearch(double latStart, double latEnd, double lonStart, double lonEnd);
    public class GeoBoundsService :IGeoBoundsService
    {
        //широта север юг
        public const double MIN_LAT = 38.0;
        public const double MAX_LAT = 82.0;

        //долгота запад-восток
        public const double MIN_LON = 19.0;
        //  public const double MIN_LON = 28.0;
        // public const double MAX_LON = 75.0;// берем только западную часть
        public const double MAX_LON = 180.0;

        // Размер квадрата 
        public const double EFFECTIVE_STEP = 3.0;


        /*
         
         координаты калининграда
        lat 54-55 
        lon 19-22
         */


        
        public ConcurrentQueue<SquareSearch> GetCoordianates(double latStart=MIN_LAT, double latBorder = MAX_LAT, double lonStart = MIN_LON, double lonBorder = MAX_LON, double step = EFFECTIVE_STEP)
        {
            ConcurrentQueue<SquareSearch> coordinates= new ConcurrentQueue<SquareSearch>();

         
            for (double lat = latStart; lat < latBorder; lat += step)
            {
                double latEnd = Math.Min(lat + step, latBorder);

                for (double lon = lonStart; lon < lonBorder; lon += step)
                {
                    double lonEnd = Math.Min(lon + step, lonBorder);
                    coordinates.Enqueue(new SquareSearch(lat, latEnd, lon, lonEnd));

                }
            }

            return coordinates;
        }


    }
}
