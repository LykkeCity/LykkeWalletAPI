using Common.Cache;
using Lykke.Service.Settings.Client;
using LykkeApi2.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace LykkeApi2.Infrastructure
{
    public class DisableOnMaintenanceFilter : ActionFilterAttribute
    {
        public const string IsOnMaintenanceCacheKey = "globalsetting-is-on-maintenance";

        private readonly ICacheManager _cacheManager;
        private readonly ISettingsClient _settingsClient;
                         
        public DisableOnMaintenanceFilter(ICacheManager cacheManager,
            ISettingsClient appGlobalSettings)
        {
            _cacheManager = cacheManager;
            _settingsClient = appGlobalSettings;
        }

        //TODO: fix
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (_cacheManager.Get(IsOnMaintenanceCacheKey, 1, async () => (await _settingsClient.GetIsOnMaintenanceAsync()).IsOnMaintenance.GetValueOrDefault()).Result)
            {
                ReturnOnMaintenance(context);
            }
        }

        private void ReturnOnMaintenance(ActionExecutingContext actionContext)
        {
            var response = ResponseModel.CreateFail(ErrorCodeType.MaintananceMode,
                "Sorry, application is on maintenance. Please try again later.");
            actionContext.Result = new JsonResult(response);
        }
    }
}