using TemplateApi.Models;
using System.Threading.Tasks;

namespace TemplateApi.Services
{
    public interface IMyServices
    {
        Task<string> GetCode(string type);
        
    }
}
