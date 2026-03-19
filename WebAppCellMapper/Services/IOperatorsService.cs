using WebAppCellMapper.Data.Models;

namespace WebAppCellMapper.Services
{
    public interface IOperatorsService
    {
        public void SaveOperators();
        public Task<List<Operator>> GetOperators();
    }
}
