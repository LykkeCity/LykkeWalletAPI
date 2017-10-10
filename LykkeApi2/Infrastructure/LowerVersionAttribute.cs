using Common;
using LykkeApi2.Models;
using LykkeApi2.Strings;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Linq;
using LykkeApi2.Infrastructure.Extensions;

namespace LykkeApi2.Infrastructure
{
    public class LowerVersionAttribute : ActionFilterAttribute
    {
        private string[] DevicesArray { get; set; }

        private string _device;
        public string Devices
        {
            get
            {
                return _device;
            }
            set
            {
                _device = value;
                DevicesArray = value.Split(',').Select(x => x.Trim().ToLower()).ToArray();
            }
        }
        public int LowerVersion { get; set; }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            string device = string.Empty;
            bool notSupported = false;
            try
            {
                //e.g. "User-Agent: DeviceType=iPhone;AppVersion=112"
                var userAgent = context.HttpContext.Request.GetUserAgent().ToLower();

                if (IsValidUserAgent(userAgent))
                {
                    var parametersDict = UserAgentHelper.ParseUserAgent(userAgent);

                    if (DevicesArray.Contains(parametersDict[UserAgentVariablesLowercase.DeviceType]))
                    {
                        if (parametersDict[UserAgentVariablesLowercase.AppVersion].ParseAnyDouble() < LowerVersion)
                        {
                            device = parametersDict[UserAgentVariablesLowercase.DeviceType];
                            notSupported = true;
                        }
                    }
                }
            }
            catch
            {
                notSupported = true;
            }
            finally
            {
                if (notSupported)
                    ReturnNotSupported(context, device);
            }
        }

        private bool IsValidUserAgent(string userAgent)
        {
            return userAgent.Contains(UserAgentVariablesLowercase.DeviceType) && userAgent.Contains(UserAgentVariablesLowercase.AppVersion);
        }

        private void ReturnNotSupported(ActionExecutingContext actionContext, string device)
        {
            string msg = device == DeviceTypesLowercase.Android ? Phrases.AndroidUpdateNeededMsg : Phrases.DefaultUpdateNeededMsg;
            var response = ResponseModel.CreateFail(ResponseModel.ErrorCodeType.VersionNotSupported, msg);
            actionContext.Result = new JsonResult(response);
        }
    }
}