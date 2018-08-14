using System.Threading.Tasks;

namespace Core.Repositories
{
    public interface IHistoryExportsRepository
    {
        Task Add(string clientId, string id, string url);
        Task<string> GetUrl(string clientId, string id);
        Task Remove(string clientId, string id);
    }
}