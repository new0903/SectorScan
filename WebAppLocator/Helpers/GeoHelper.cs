using WebAppLocator.DTO;

namespace WebAppLocator.Helpers
{
    public  class GeoHelper
    {

        //с этими показателями надо поиграть может быть точность можно сделать ещё лучше.
        public const double N = 10.0; // Коэффициент затухания (город) 2.0
        public const double A = -34;// RSSI на 1 метре 35
        public const double LPS = 60;// RSSI для старых позиций, если слишком маленький то точка будет стоять на месте.
        //если N слишком маленький (например 2) то бс с самым большим сигналом тупо притянет к себе точку.


        public const double METERS_PER_DEGREE = 111111.0;
        public const double R = 6371000; // Радиус Земли в метрах

        public  double DistancePerDegree(double latS, double lonS, double latE, double lonE) => Math.Sqrt(Math.Pow(latS-latE,2) +Math.Pow(lonS-lonE,2));
        


        public double rad(double angle) => angle * Math.PI / 180.0;

        public double DistancePerMeters(double latS, double lonS, double latE, double lonE)
        {


            double dLat = rad(latE - latS);
            double dLon = rad(lonE - lonS);

            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                       Math.Cos(rad(latS)) * Math.Cos(rad(latE)) *
                       Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return R * c; // метры
        }

        public double DistancePerMeters(double distanceDegree)=> METERS_PER_DEGREE * distanceDegree;//грубо ну да пофиг
        

        public double GetBearingDegrees(double latA, double lonA, double latB, double lonB)
        {
            // Переводим в радианы
            double lat1 = rad(latA);
            double lat2 = rad(latB);
            double dLon = rad(lonB - lonA);

            // Формула азимута
            double y = Math.Sin(dLon) * Math.Cos(lat2);
            double x = Math.Cos(lat1) * Math.Sin(lat2) - Math.Sin(lat1) * Math.Cos(lat2) * Math.Cos(dLon);

            double bearingRad = Math.Atan2(y, x);

            // В градусы и нормализация в диапазон [0, 360)
            double bearingDeg = bearingRad * 180.0 / Math.PI;
            return (bearingDeg + 360) % 360;
        }

        /// <summary>
        /// Находит координату между точками.
        /// </summary>
        /// <param name="lat"></param>
        /// <param name="lon"></param>
        /// <param name="distanceMeters"></param>
        /// <param name="bearingDegrees"></param>
        /// <returns></returns>
        public  LocationPoint OffsetByMeters(double lat, double lon, double distanceMeters, double bearingDegrees)
        {
            // Переводим в радианы
            double latRad = lat * Math.PI / 180.0;
            double bearingRad = bearingDegrees * Math.PI / 180.0;

            // Угловое расстояние в радианах
            double angularDistance = distanceMeters / R;

            // Формула сферической тригонометрии
            double newLatRad = Math.Asin(
                Math.Sin(latRad) * Math.Cos(angularDistance) +
                Math.Cos(latRad) * Math.Sin(angularDistance) * Math.Cos(bearingRad)
            );

            double newLonRad = lon * Math.PI / 180.0 + Math.Atan2(
                Math.Sin(bearingRad) * Math.Sin(angularDistance) * Math.Cos(latRad),
                Math.Cos(angularDistance) - Math.Sin(latRad) * Math.Sin(newLatRad)
            );

            // Обратно в градусы
            double newLat = newLatRad * 180.0 / Math.PI;
            double newLon = newLonRad * 180.0 / Math.PI;

            return new LocationPoint(newLat, newLon);
        }
    }
}
