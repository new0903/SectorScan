
namespace ConsoleAppMapGrid
{
    public record SquareSearch(double latStart, double latEnd, double lonStart, double lonEnd);
    internal class Program
    {  
        // Границы России (приблизительно)
        //широта север-юг
        const double MIN_LAT = 41.0;
        const double MAX_LAT = 82.0;

        //долгота запад-восток
        const double MIN_LON = 19.0;
     //   const double MAX_LON = 180.0;
        const double MAX_LON = 75.0;

        // Размер квадрата и перекрытие
        const double STEP = 2.0;
        const double OVERLAP = 0.4;
        const double EFFECTIVE_STEP = STEP - OVERLAP; // 1.6 градуса




        static List<SquareSearch> squareSearches = new List<SquareSearch>();


        public static void GetCoordianates()
        {

            for (double lat = MIN_LAT; lat < MAX_LAT; lat += EFFECTIVE_STEP)
            {
                double latEnd = Math.Min(lat + EFFECTIVE_STEP, MAX_LAT);

                for (double lon = MIN_LON; lon < MAX_LON; lon += EFFECTIVE_STEP)
                {
                    double lonEnd = Math.Min(lon + EFFECTIVE_STEP, MAX_LON);



                    Console.Write($"\nlatStart={lat}&latEnd={latEnd}&lonStart={lon}&lonEnd={lonEnd}");


                    squareSearches.Add(new SquareSearch(lat, latEnd, lon, lonEnd));
                }
            }

            Console.WriteLine($"Готово! Всего квадратов: {squareSearches.Count}");
        }



        static void Main(string[] args)
        {
            Console.WriteLine("Hello, World!");
            GetCoordianates();

            Console.ReadKey();
        }
    }
}
