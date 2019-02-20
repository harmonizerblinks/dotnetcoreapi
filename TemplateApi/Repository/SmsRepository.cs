using TemplateApi.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TemplateApi.Repository
{
    public class SmsRepository : GenericRepository<Sms>, ISmsRepository
    {
        public SmsRepository(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }
        
    }
}
