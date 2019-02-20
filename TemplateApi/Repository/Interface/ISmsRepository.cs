﻿using TemplateApi.Models;
using System.Linq;
using System.Threading.Tasks;

namespace TemplateApi.Repository
{
    public interface ISmsRepository : IGenericRepository<Sms>
    {
        IQueryable<Sms> GetAll();
        IQueryable<SmsBoardcast> GetBroadcast();
        Task<SmsBoardcast> InsertBroadcast(SmsBoardcast sms);
    }
}
