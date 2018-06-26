using System.Net;
using System.Threading.Tasks;
using AutoMapper;
using Common.Log;
using Core.Constants;
using Core.Domain.Recovery;
using Core.Dto.Recovery;
using Core.Exceptions;
using Core.Services.Recovery;
using Lykke.Service.ClientAccount.Client;
using Lykke.Service.ClientAccountRecovery.Client;
using Lykke.Service.ClientAccountRecovery.Client.AutoRestClient.Models;
using Lykke.Service.PersonalData.Client;
using Lykke.Service.Session.AutorestClient.Models;
using Lykke.Service.Session.Client;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json.Linq;
using Refit;

namespace LkeServices.Recovery
{
    /// <inheritdoc />
    public class ClientAccountRecoveryService : IClientAccountRecoveryService
    {
        private readonly ILog _log;
        private readonly IMapper _mapper;
        private readonly IClientAccountClient _clientAccountClient;
        private readonly IAccountRecoveryService _accountRecoveryService;
        private readonly IClientSessionsClient _clientSessionsClient;
        private readonly IPersonalDataClientAccountRecoveryClient _personalDataClientAccountRecoveryClient;

        public ClientAccountRecoveryService(
            ILog log,
            IMapper mapper,
            IClientSessionsClient clientSessionsClient,
            IAccountRecoveryService accountRecoveryService,
            IClientAccountClient clientAccountClient,
            IPersonalDataClientAccountRecoveryClient personalDataClientAccountRecoveryClient)
        {
            _log = log;
            _mapper = mapper;
            _clientSessionsClient = clientSessionsClient;
            _accountRecoveryService = accountRecoveryService;
            _clientAccountClient = clientAccountClient;
            _personalDataClientAccountRecoveryClient = personalDataClientAccountRecoveryClient;
        }

        /// <inheritdoc />
        public async Task<string> StartRecoveryAsync(RecoveryStartDto recoveryStartDto)
        {
            try
            {
                var clientByEmail = await _clientAccountClient.GetClientByEmailAndPartnerIdAsync(recoveryStartDto.Email, recoveryStartDto.PartnerId);

                if (clientByEmail == null)
                {
                    // TODO:@gafanasiev decide what message to pass instead of hardcoded string.
                    throw LykkeApiErrorException.NotFound(
                        LykkeApiErrorCodes.Service.ClientNotFound);
                }

                var newRecoveryRequest = _mapper.Map<RecoveryStartDto, NewRecoveryRequest>(recoveryStartDto,
                    opt => opt.Items["ClientId"] = clientByEmail.Id);

                var newRecoveryResponse =
                    await _accountRecoveryService.StartNewRecoveryAsync(newRecoveryRequest);

                var recoveryId = newRecoveryResponse.RecoveryId;

                var recoveryStatusResponse = await _accountRecoveryService.GetRecoveryStatusAsync(recoveryId);

                var newState = new RecoveryState
                {
                    Email = recoveryStartDto.Email,
                    RecoveryId = recoveryId,
                    Challenge = recoveryStatusResponse.Challenge
                };

                var stateToken = await GenerateEncryptedTokenAsync(newState, JwtTypeName.Default);

                return stateToken;
            }
            catch (ForbiddenException e)
            {
                // TODO:@gafanasiev decide what message to pass instead of e.Message.
                throw LykkeApiErrorException.Forbidden(
                    LykkeApiErrorCodes.Service.RecoveryStartAttemptLimitReached,
                    e.Message);
            }
        }

        /// <inheritdoc />
        public async Task<RecoveryStatus> GetRecoveryStatusAsync(string stateToken)
        {
            var recoveryId = await GetRecoveryIdAsync(stateToken);

            var recoveryStatusResponse = await _accountRecoveryService.GetRecoveryStatusAsync(recoveryId);

            var result = _mapper.Map<RecoveryStatusResponse, RecoveryStatus>(recoveryStatusResponse);

            return result;
        }

        /// <inheritdoc />
        public async Task<string> SubmitChallengeAsync(RecoverySubmitChallengeDto recoverySubmitChallengeDto)
        {
            var oldState = await GetTokenPayloadAsync<RecoveryState>(recoverySubmitChallengeDto.StateToken);
            var recoveryId = oldState.RecoveryId;
            var email = oldState.Email;

            var challengeRequest = _mapper.Map<RecoverySubmitChallengeDto, ChallengeRequest>(recoverySubmitChallengeDto,
                opt =>
                {
                    opt.Items["Challenge"] = oldState.Challenge;
                    opt.Items["RecoveryId"] = oldState.RecoveryId;
                });

            var operationStatus = await _accountRecoveryService.SubmitChallengeAsync(challengeRequest);

            if (operationStatus.Error)
            {
                // TODO:@gafanasiev decide what message to pass instead of operationStatus.Message.
                throw LykkeApiErrorException.BadRequest(
                    LykkeApiErrorCodes.Service.RecoverySubmitChallengeInvalidValue,
                    operationStatus.Message);
            }

            var newRecoveryStatusResponse =
                await _accountRecoveryService.GetRecoveryStatusAsync(recoveryId);

            var newChallenge = newRecoveryStatusResponse.Challenge;

            var newState = new RecoveryState
            {
                Challenge = newChallenge,
                Email = email,
                RecoveryId = recoveryId
            };

            var newTokenType = GetTokenType(newRecoveryStatusResponse);

            var newStateToken = await GenerateEncryptedTokenAsync(newState, newTokenType);

            return newStateToken;
        }

        /// <inheritdoc />
        public async Task<string> UploadSelfieFileAsync(IFormFile image)
        {
            try
            {
                using (var imageStream = image.OpenReadStream())
                {
                    var streamPart = new StreamPart(imageStream, image.FileName, image.ContentType);
                    var fileId = await _personalDataClientAccountRecoveryClient.UploadSelfieAsync(streamPart);
                    return fileId;
                }
            }
            catch (ApiException e)
            {
                _log.WriteInfoAsync(
                    nameof(ClientAccountRecoveryService), 
                    nameof(UploadSelfieFileAsync),
                    $"FileName: {image.FileName}; Length: {image.Length} bytes; ContentType: {image.ContentType};",
                    e.Message);

                switch (e.StatusCode)
                {
                    // TODO:@gafanasiev decide what message to pass instead of e.Message.
                    case HttpStatusCode.BadRequest:
                        throw LykkeApiErrorException.BadRequest(
                            LykkeApiErrorCodes.Service.RecoveryUploadInvalidSelfieFile,
                            e.Message);
                    default:
                        throw;
                }
            }
        }

        /// <inheritdoc />
        public async Task CompleteRecoveryAsync(RecoveryCompleteDto recoveryCompleteDto)
        {
            var recoveryId = await GetRecoveryIdAsync(recoveryCompleteDto.StateToken);

            try
            {
                var passwordRequest = _mapper.Map<RecoveryCompleteDto, PasswordRequest>(recoveryCompleteDto,
                    opt =>
                    {
                        opt.Items["RecoveryId"] = recoveryId;
                    });

                await _accountRecoveryService.UpdatePasswordAsync(passwordRequest);
            }
            catch (BadRequestException e)
            {
                // We need log here, because we don't want to decode token one more time in controller.
                _log.WriteInfoAsync(
                    nameof(ClientAccountRecoveryService),
                    nameof(CompleteRecoveryAsync),
                    $"RecoveryId: {recoveryId};", 
                    e.Message);

                // TODO:@gafanasiev decide what message to pass instead of e.Message.
                throw LykkeApiErrorException.BadRequest(
                    LykkeApiErrorCodes.Service.RecoveryCompleteFailedInvalidData, 
                    e.Message);
            }       
        }

        private async Task<T> GetTokenPayloadAsync<T>(string stateToken)
        {
            var jwtDecodeRequest = new JwtDecodeRequest(stateToken);
            var jwtDecodeResponse =
                await _clientSessionsClient.JwtDecodeAsync(jwtDecodeRequest);

            return JObject.FromObject(jwtDecodeResponse.JwtData.Payload).ToObject<T>();
        }

        private async Task<string> GetRecoveryIdAsync(string stateToken)
        {
            var payload = await GetTokenPayloadAsync<RecoveryState>(stateToken);
            return payload.RecoveryId;
        }

        private async Task<string> GenerateEncryptedTokenAsync(object payload, JwtTypeName tokenType)
        {
            var jwtData = new JwtData(null, payload);
            var jwtGenerateRequest = new JwtGenerateRequest(true, tokenType, jwtData);

            var jwtGenerateResponse = await _clientSessionsClient.JwtGenerateAsync(jwtGenerateRequest);
            return jwtGenerateResponse.Token;
        }

        private JwtTypeName GetTokenType(RecoveryStatusResponse recoveryStatusResponse)
        {
            // If recovery process is frozen we should generate an infinite token.
            if (recoveryStatusResponse.OverallProgress == Progress.Frozen) 
                return JwtTypeName.Infinite;

            switch (recoveryStatusResponse.Challenge)
            {
                // If challenge could take a lot of time to complete we should generate an infinite token.
                case Challenge.Selfie:
                    return JwtTypeName.Infinite;
                default:
                    return JwtTypeName.Default;
            }
        }
    }
}