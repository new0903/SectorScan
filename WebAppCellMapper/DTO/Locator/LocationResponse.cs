namespace WebAppCellMapper.DTO.Locator
{

    public record LocationResponse(LocationPoint point, double accuracy);

    public record LocationPoint(double lat,  double lon);

    
}
