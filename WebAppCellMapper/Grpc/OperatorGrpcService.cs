using Grpc.Core;
using WebAppCellMapper.Services;

namespace WebAppCellMapper.Grpc
{
    public class OperatorGrpcService : OperatorServiceGrpc.OperatorServiceGrpcBase
    {
        private readonly IOperatorsService service;

        public OperatorGrpcService(IOperatorsService service) 
        {
            this.service = service;
        }
        public override async Task<OperatorsResponse> GetOperators(EmptyRequest request, ServerCallContext context)
        {
            var list=await service.GetOperators();
            OperatorsResponse response=new OperatorsResponse();
            response.Data.AddRange(list.Select(o=>new OperatorResponse() {
                Id=o.Id,
                InternalCode=o.InternalCode,
                Name=o.Name,
                VisibleCode=o.VisibleCode,
            }));
            return response;
        }
    }
}
