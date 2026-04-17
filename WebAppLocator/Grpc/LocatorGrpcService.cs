
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using WebAppCellMapper.Services;
using WebAppLocator.DTO;

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

            var LocationRequest = new LocationRequest();
            var res = await locatorService.FindLocation(LocationRequest, user.Identity.Name);
            LocationResponseGrpc model= new LocationResponseGrpc();
            var result=res.First();
            model.Accuracy = result.accuracy;
            model.Point.Lat=result.point.lat;
            model.Point.Lon = result.point.lon;

            return model;
        }
    }
}
