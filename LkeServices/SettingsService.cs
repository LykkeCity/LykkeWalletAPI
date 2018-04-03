using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Core.Exchange;
using Core.GlobalSettings;
using Core.Services;

namespace LkeServices
{
    public class SettingsService : ISettingsService
    {
        private readonly IExchangeSettingsRepository _exchangeSettingsRepository;
        private readonly IAppGlobalSettingsRepository _appGlobalSettingsRepository;

        public SettingsService(IExchangeSettingsRepository exchangeSettingsRepository, 
            IAppGlobalSettingsRepository appGlobalSettingsRepository)
        {
            _exchangeSettingsRepository = exchangeSettingsRepository;
            _appGlobalSettingsRepository = appGlobalSettingsRepository;
        }

        public Task<IExchangeSettings> GetExchangeSettingsAsync(string clientId)
        {
            return _exchangeSettingsRepository.GetFromDbOrDefaultAsync(clientId);
        }

        public Task<IAppGlobalSettings> GetAppGlobalSettingsSettingsAsync()
        {
            return _appGlobalSettingsRepository.GetFromDbOrDefault();
        }
    }
}
