using System.Collections.Generic;
using System.Threading.Tasks;

namespace Core.Repositories
{
    public interface IFeaturesRepository
    {
        Task<IDictionary<string, bool>> GetAll(string clientId = null);
    }
}
