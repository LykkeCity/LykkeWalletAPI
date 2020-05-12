using System.Net;
using System.Threading.Tasks;
using Core.Constants;
using Lykke.Service.ConfirmationCodes.Client;
using LykkeApi2.Models._2Fa;
using Refit;

namespace LykkeApi2.Services
{
    public class Google2FaService
    {
        private readonly IConfirmationCodesClient _confirmationCodesClient;

        public Google2FaService(
            IConfirmationCodesClient confirmationCodesClient
            )
        {
            _confirmationCodesClient = confirmationCodesClient;
        }
        public async Task<Google2FaResultModel<T>>Check2FaAsync<T>(string clientId, string code)
        {
            try
            {
                bool codeIsValid = await _confirmationCodesClient.Google2FaCheckCodeAsync(clientId, code);

                if (!codeIsValid)
                    return Google2FaResultModel<T>.Fail(
                        LykkeApiErrorCodes.Service.SecondFactorCodeIncorrect.Name,
                        LykkeApiErrorCodes.Service.SecondFactorCodeIncorrect.DefaultMessage);

                return null;
            }
            catch (ApiException e)
            {
                switch (e.StatusCode)
                {
                    case HttpStatusCode.BadRequest:
                        return Google2FaResultModel<T>.Fail(
                            LykkeApiErrorCodes.Service.TwoFactorRequired.Name,
                            LykkeApiErrorCodes.Service.TwoFactorRequired.DefaultMessage);
                    case HttpStatusCode.Forbidden:
                        return Google2FaResultModel<T>.Fail(
                            LykkeApiErrorCodes.Service.SecondFactorCheckForbiden.Name,
                            LykkeApiErrorCodes.Service.SecondFactorCheckForbiden.DefaultMessage);
                }

                throw;
            }
        }
    }
}
