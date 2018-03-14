using Common.Cache;
using Core.GlobalSettings;
using LykkeApi2.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace LykkeApi2.Infrastructure
{
    public class DisableOnMaintenanceFilter : ActionFilterAttribute
    {
        public const string IsOnMaintenanceCacheKey = "globalsetting-is-on-maintenance";

        private readonly ICacheManager _cacheManager;
        private readonly IAppGlobalSettingsRepository _appGlobalSettings;
                         
        public DisableOnMaintenanceFilter(ICacheManager cacheManager, IAppGlobalSettingsRepository appGlobalSettings)
        {
            _cacheManager = cacheManager;
            _appGlobalSettings = appGlobalSettings;
        }

        //TODO: fix
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (_cacheManager.Get(IsOnMaintenanceCacheKey, 1, async () => (await _appGlobalSettings.GetFromDbOrDefault()).IsOnMaintenance).Result)
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