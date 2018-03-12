using System.Threading.Tasks;

namespace Core.Exchange
{
    public interface IExchangeSettingsRepository
    {
        Task<IExchangeSettings> GetOrDefaultAsync(string clientId);
        Task<IExchangeSettings> GetAsync(string clientId);
    }
}