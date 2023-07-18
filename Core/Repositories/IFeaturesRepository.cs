using System.Collections.Generic;
using System.Threading.Tasks;

namespace Core.Repositories
{
    public interface IFeaturesRepository
    {
        Task AddOrUpdate(string featureName, bool value, string clientId = null);

        Task<IDictionary<string, bool>> GetAll(string clientId = null);
    }
}
