using System.Threading.Tasks;

namespace Core.Exchange
{
    public interface IExchangeSettingsRepository
    {
        Task<IExchangeSettings> GetFromDbOrDefaultAsync(string clientId);
        Task<IExchangeSettings> GetAsync(string clientId);
    }
}