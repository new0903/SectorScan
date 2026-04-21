using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Xml.Serialization;
using WebAppCellMapper.DTO.Locator;
using WebAppCellMapper.DTO.Locator.GPX;
using WebAppCellMapper.Options;

namespace WebAppCellMapper.Helpers
{
    public class GraphHopperService : IGraphHopperService
    {
        private readonly IHttpClientFactory clientFactory;
        private readonly GraphOptions options;
        private readonly ILogger<GraphHopperService> logger;

        public string ApiKey => "7c52eb48-8b00-4fa0-b921-039551d184da";

        public GraphHopperService(IHttpClientFactory clientFactory, IOptions<GraphOptions> options, ILogger<GraphHopperService> logger)
        {
            this.clientFactory = clientFactory;
            this.options = options.Value;
            this.logger = logger;
        }
   
        public async Task<List<LocationPoint>> MatchRoadAsync(List<LocationPoint> points, CancellationToken cancellationToken = default)
        {
            List<LocationPoint> listCoord =new List<LocationPoint>(); 
            if (!options.Use)
            {
                return listCoord;
            }
            try
            {
                

                // Формируем строку с координатами в формате "lon,lat"
                var bodyRequest = new TrackRequest()
                {
                    Profile = "car_only",
                    Elevation = false,
                    Instructions = false,
                    Locale = "ru",
                    PointsEncodedMultiplier = 1000000,
                    Details = ["road_class", "road_environment", "max_speed", "average_speed"],
                    SnapPreventions = ["ferry", "ford"],//"ferry"
                    PointsEncoded = false,
                    Points = points.Select(p => new double[] { p.lon, p.lat }).ToArray()
                };

                var jsonContentPath = JsonSerializer.Serialize(bodyRequest);


                var content = new StringContent(jsonContentPath, Encoding.UTF8, "application/json");
                var url = $"route?key=";
                var client = clientFactory.CreateClient("Graph");
                var response = await client.PostAsync(url, content, cancellationToken);
                if (response.IsSuccessStatusCode)
                {

                    var jsonResponse = await response.Content.ReadAsStringAsync(cancellationToken);
                    var resContent = JsonSerializer.Deserialize<GpTrackResponse>(jsonResponse);
                    if (resContent == null) return listCoord;
                    var path = resContent.Paths.FirstOrDefault();
                    if (path == null) return listCoord;
                    listCoord = path.Points.Coordinates.Select(p => new LocationPoint(p[1], p[0])).ToList();
                    return listCoord;

                }
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                logger.LogInformation(errorBody);
            }
            catch (Exception ex)
            {
                logger.LogInformation(ex.Message);
            }

            return listCoord;
        }

    }
}
