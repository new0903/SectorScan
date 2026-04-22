namespace WebAppCellMapper.DTO.Locator
{

    public class LocationAnswer { 
        public LocationResponse location {  get; set; }
    }

    public record LocationResponse(LocationPoint point, double accuracy);

    public record LocationPoint(double lat,  double lon);

    
}
