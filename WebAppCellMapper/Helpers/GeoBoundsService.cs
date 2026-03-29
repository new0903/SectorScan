

namespace WebAppCellMapper.Helpers
{
    //public record SquareSearch(double latStart, double latEnd, double lonStart, double lonEnd, bool isScanned);
 
    public class SquareSearch
    {
        public double LatStart { get; init; }
        public double LatEnd { get; init; }
        public double LonStart { get; init; }
        public double LonEnd { get; init; }
        public bool IsScanned { get; set; }

        public SquareSearch()
        {

        }
        public SquareSearch(double latStart, double latEnd, double lonStart, double lonEnd, bool isScanned=false)
        {
            LatStart = latStart;
            LatEnd = latEnd;
            LonStart = lonStart;
            LonEnd = lonEnd;
            IsScanned = isScanned;
        }
    }
    public class GeoBoundsService :IGeoBoundsService
    {
        //широта север юг
        public const double MIN_LAT = 38.289338424253174;
        public const double MAX_LAT = 82.94691939841713;

        //долгота запад-восток
        public const double MIN_LON = 19.17218236265085;
        public const double MAX_LON = 170.4246613353112;

        // Размер квадрата 
        public const double EFFECTIVE_STEP = 13.5345823353182;


     

        
        public List<SquareSearch> GetCoordianates(double latStart=MIN_LAT, double latBorder = MAX_LAT, double lonStart = MIN_LON, double lonBorder = MAX_LON, double step = EFFECTIVE_STEP)
        {
            List<SquareSearch> coordinates= new List<SquareSearch>();

         
            for (double lat = latStart; lat < latBorder; lat += step)
            {
                double latEnd = Math.Min(lat + step, latBorder);

                for (double lon = lonStart; lon < lonBorder; lon += step)
                {
                    double lonEnd = Math.Min(lon + step, lonBorder);
                    coordinates.Add(new SquareSearch(lat, latEnd, lon, lonEnd));

                }
            }

            return coordinates;
        }


    }
}
