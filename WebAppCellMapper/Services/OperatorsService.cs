using Domain.Models;
using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
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
                if (context.operators.Any()) return;

                var filePath = Path.Combine(env.ContentRootPath, "operators.json");
                var jsonContent = File.ReadAllText(filePath);

                var countryList = JsonSerializer.Deserialize<List<Country>>(jsonContent);
                //надо будет уточнить насчет других стран
                if (countryList == null) return;
                var operators = countryList.FirstOrDefault(c => c.countryId == 1);
                if (operators == null) return;
                var processedOperators = operators.operators.Select(op =>
                {
                    if (string.IsNullOrWhiteSpace(op.Name))
                    {
                        op.Name = $"Operator_{op.InternalCode}"; 
                    }
                    return op;
                }).ToList();
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
