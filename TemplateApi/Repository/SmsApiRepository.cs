using TemplateApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TemplateApi.Repository
{
    public class SmsApiRepository : GenericRepository<SmsApi>, ISmsApiRepository
    {
        public SmsApiRepository(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }
    }
}
