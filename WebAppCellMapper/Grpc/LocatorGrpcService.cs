
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using WebAppCellMapper.DTO.Locator;
using WebAppCellMapper.DTO.Locator.Cells;
using WebAppCellMapper.Services;

namespace WebAppCellMapper.Grpc
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

            var result = await locatorService.FindLocation(locationRequest, user.Identity.Name);
            if (result==null)
            {
                var status = new Status(StatusCode.InvalidArgument, "not found");
                throw new RpcException(status);
            }

            LocationResponseGrpc model = new LocationResponseGrpc();
            model.Accuracy = result.location.accuracy;
            model.Point.Lat=result.location.point.lat;
            model.Point.Lon = result.location.point.lon;

            return model;
        }
    }
}
