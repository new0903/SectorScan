using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using WebAppCellMapper.Data;
using WebAppCellMapper.Data.Models;

namespace WebAppCellMapper.Services
{
    public class OperatorsService : IOperatorsService
    {
        private readonly IWebHostEnvironment env;
        private readonly ILogger<OperatorsService> logger;
        private readonly AppDBContext context;

        public OperatorsService(IWebHostEnvironment env, ILogger<OperatorsService> logger, AppDBContext context)
        {
            this.env = env;
            this.logger = logger;
            this.context = context;
        }
        public void SaveOperators()
        {
            try
            {
                //проверяю есть ли вообще операторы
                if (context.operators.Any()) return;

                var filePath = Path.Combine(env.ContentRootPath, "operators.json");//можно http запрос делать
                var jsonContent = File.ReadAllText(filePath);


                var countryList = JsonConvert.DeserializeObject<List<Country>>(jsonContent);
                //надо будет уточнить насчет других стран
                if (countryList == null) return;
                var operators = countryList.FirstOrDefault(c => c.countryId == 1);
                if (operators == null) return;
                var processedOperators = operators.operators.Select(op =>
                {
                    if (string.IsNullOrWhiteSpace(op.Name))
                    {
                        op.Name = $"Operator_{op.InternalCode}"; //эти падлы не везде name поставили 
                    }
                    return op;
                }).ToList();
                //сохраняем все в бд
                context.BulkInsertOrUpdate(processedOperators);
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
            }

        }

        public async Task<List<Operator>> GetOperators()
        {
            var operators=await context.operators.AsNoTracking().ToListAsync();
            return operators;
        }


    }
}
