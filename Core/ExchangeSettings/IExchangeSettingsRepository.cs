using System.Threading.Tasks;

namespace Core.ExchangeSettings
{
    public interface IExchangeSettingsRepository
    {
        Task<IExchangeSettings> GetOrDefaultAsync(string clientId);
    }
}