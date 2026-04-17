namespace WebAppLocator.DTO
{

    public record LocationResponse(LocationPoint point, double accuracy,string title);

    public record LocationPoint(double lat,  double lon);

    
}
