using TemplateApi.Models;
using System.Threading.Tasks;

namespace TemplateApi.Repository
{
    public interface ISequenceRepository : IGenericRepository<Sequence>
    {
        Task<string> GetCode(string type);

        Task<string> GetCode(int id);
    }
}
