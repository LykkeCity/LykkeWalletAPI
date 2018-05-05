using System.Threading.Tasks;
using Core.Exchange;
using Core.GlobalSettings;

namespace Core.Services
{
    public interface ISettingsService
    {
        Task<IExchangeSettings> GetExchangeSettingsAsync (string clientId);
        Task<IAppGlobalSettings> GetAppGlobalSettingsSettingsAsync();
    }
}