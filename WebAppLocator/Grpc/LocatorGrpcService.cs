
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using WebAppCellMapper.Services;
using WebAppLocator.DTO;
using WebAppLocator.DTO.Cells;

namespace WebAppLocator.Grpc
{
    [Authorize]
    public class LocatorGrpcService : LocatorServiceGrpc.LocatorServiceGrpcBase
    {
        private readonly ILocatorService locatorService;

        public LocatorGrpcService(ILocatorService locatorService)
        {
            this.locatorService = locatorService;
        }

        public override async Task<LocationResponseGrpc> GetLocate(LocationRequestGrpc request, ServerCallContext context)
        {
            var httpContext = context.GetHttpContext();
            var user = httpContext.User;

            var locationRequest = new LocationRequest();
            locationRequest.Cell = request.Cell
                .Select(c=>
                new CellRequest() {
                    Gsm=c.Gsm!=null?  new GSMDataCell(c.Gsm.Mcc, c.Gsm.Mnc, c.Gsm.SignalStrength,c.Gsm.Lac,c.Gsm.Cid) : null,
                    Wcdma = c.Wcdma != null ? new GSMDataCell(c.Wcdma.Mcc, c.Wcdma.Mnc, c.Wcdma.SignalStrength, c.Wcdma.Lac, c.Wcdma.Cid) : null,
                    Lte = c.Lte != null ? new LteDataCell(c.Lte.Mcc,c.Lte.Mnc,c.Lte.SignalStrength,c.Lte.Tac,c.Lte.Ci) : null,
                    Nr = c.Nr != null ? new LteDataCell(c.Nr.Mcc, c.Nr.Mnc, c.Nr.SignalStrength, c.Nr.Tac, c.Nr.Ci) : null,
                })
                .ToArray();

            var res = await locatorService.FindLocation(locationRequest, user.Identity.Name);
            LocationResponseGrpc model= new LocationResponseGrpc();
            var result=res.First();
            model.Accuracy = result.accuracy;
            model.Point.Lat=result.point.lat;
            model.Point.Lon = result.point.lon;

            return model;
        }
    }
}
