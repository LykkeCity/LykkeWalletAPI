﻿using Common;
using Common.Log;
using Lykke.MarketProfileService.Client;
using Lykke.Service.Assets.Client.Custom;
using LykkeApi2.Models;
using LykkeApi2.Models.AssetPairRates;
using LykkeApi2.Models.AssetPairsModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LykkeApi2.Controllers
{
    [Route("api/[controller]")]
    public class AssetPairsController : Controller
    {
        private readonly CachedDataDictionary<string, IAssetPair> _assetPairs;
        private readonly ICachedAssetsService _assetsService;
        private readonly ILykkeMarketProfileServiceAPI _marketProfileService;
        private readonly ILog _log;

        public AssetPairsController(CachedDataDictionary<string, IAssetPair> assetPairs, ICachedAssetsService assetsService, ILykkeMarketProfileServiceAPI marketProfile,  ILog log)
        {
            _assetPairs = assetPairs;
            _assetsService = assetsService;
            _marketProfileService = marketProfile;            
            _log = log;
        }

        [HttpGet]
        public async Task<ResponseModel<AssetPairResponseModel>> Get()
        {
            var assetPairs = (await _assetPairs.Values()).Where(s => !s.IsDisabled);


            return ResponseModel<AssetPairResponseModel>.CreateOk(
                AssetPairResponseModel.Create(assetPairs.Select(itm => itm.ConvertToApiModel()).ToArray()));

            //var assetPairsResult = await ExecuteMethod<ServiceReponse>(async () =>
            //{
            //    var assetPairs = (await _assetPairs.Values()).Where(s => !s.IsDisabled);
            //    return assetPairs;

            //});


            //HandleError(assetPairsResult.Error)

            //if (assetPairsResult.Error != null)
            //{
            //    return ResponseModel<AssetPairResponseModesl>.CreateFail(ResponseModel.ErrorCodeType.BadRequest, assetPairsResult.Error); //return StatusCode(StatusCodes.Status500InternalServerError);
            //}

            //return ResponseModel<AssetPairResponseModesl>.CreateOk(
            //    AssetPairResponseModesl.Create(assetPairsResult.Result.Select(itm => itm.ConvertToApiModel()).ToArray()));
        }

        [HttpGet("{id}")]
        public async Task<ResponseModel<AssetPairResponseModel>> GetAssetPairById(string id)
        {
            var assetPair = (await _assetPairs.Values()).First(x => x.Id == id);       
            return ResponseModel<AssetPairResponseModel>.CreateOk(
                AssetPairResponseModel.Create(new List<AssetPairModel> { assetPair.ConvertToApiModel() }));

           
        }
        
        [HttpGet("rates")]
        public async Task<ResponseModel<AssetPairRatesResponseModel>> GetAssetPairRates()
        {
            //TODO: get client id and partnerId from session/authorization: e.g. this.GetClientId();
            var clientId = "";
            var isIosDevice = this.IsIosDevice();
            var partnerId = "";

            //TODO - uncomment when new beta version of assets service is created
            var assetPairs = new List<AssetPairModel>(); //await _assetsService.GetAssetsPairsForClient( new Lykke.Service.Assets.Client.Models.GetAssetPairsForClientRequestModel { ClientId = clientId, IsIosDevice = isIosDevice, PartnerId = partnerId } );

            //var assetPairs = await _assetsService.GetAssetsPairsForClient( new Lykke.Service.Assets.Client.Models.GetAssetPairsForClientRequestModel { ClientId = clientId, IsIosDevice = isIosDevice, PartnerId = partnerId } );

            var assetPairsDict = assetPairs.ToDictionary(itm => itm.Id);
            var marketProfile = await _marketProfileService.ApiMarketProfileGetAsync();

            marketProfile = marketProfile.Where(itm => assetPairsDict.ContainsKey(itm.AssetPair)).ToList();

            var list = marketProfile.Select(s => s.ConvertToApiModel());
            //foreach (var feedData in marketProfile)
            //{
            //    var feedHoursHistory = await _feedHoursHistoryRepository.GetAsync(feedData.Asset);
            //    list.Add(feedData.ConvertToApiModel(marketProfile));
            //}

            //var invertedPairsSettings =
            //    await _clientSettingsRepository.GetSettings<AssetPairsInvertedSettings>(clientId);

            return ResponseModel<AssetPairRatesResponseModel>.CreateOk(AssetPairRatesResponseModel.Create(list.ToArray()));
        }

        [HttpGet("rates/{assetPairId}")]
        public async Task<ResponseModel<AssetPairRatesResponseModel>> GetAssetPairRatesById(string assetPairId)
        {
            //TODO: param validation - return bad request
            //if (string.IsNullOrEmpty(id))
            //    return ResponseModel<AssetPairRatesResponseModel>.CreateInvalidFieldError("id", Phrases.FieldShouldNotBeEmpty);

            var asset = (await _assetPairs.Values()).FirstOrDefault(x => x.Id == assetPairId);

            //TODO VALIDATION: return bad request
            //if (asset == null)
            //    return ResponseModel<GetAssetPairRateModel>.CreateInvalidFieldError("id", Phrases.InvalidValue);

            //TODO:
            var marketProfile = await _marketProfileService.ApiMarketProfileGetAsync();

            var feedData = marketProfile.FirstOrDefault(itm => itm.AssetPair == assetPairId);

            //TODO VALIDATION: 
            //if (nowFeedData == null)
            //return ResponseModel<GetAssetPairRateModel>.CreateFail(ResponseModel.ErrorCodeType.NoData, Phrases.NoData);

            //TODO: Read from candles service
            //var feedHoursHistory = await _feedHoursHistoryRepository.GetAsync(feedData.AssetPair);

            //var invertedPairsSettings = await _clientSettingsRepository.GetSettings<AssetPairsInvertedSettings>(clientId);

            //TODO: ConvertToApiModel with candles history
            //var rate = feedData.ConvertToApiModel(feedHoursHistory);
            var rate = feedData.ConvertToApiModel();
            //Lykke.MarketProfileService.Client.Models.AssetPairModel src
                        

            return ResponseModel<AssetPairRatesResponseModel>.CreateOk(AssetPairRatesResponseModel.Create(new List<AssetPairRateModel>  { rate }));
        }

























        private async Task<ResponseResult<T>> ExecuteMethod<T>(Func<Task<T>> action)
        {
            try
            {
                var res= await action();
                var response = new ResponseResult<T>();
                response.Result = res;

                return response;

            }
            catch (Exception ex)
            {
                await _log.WriteErrorAsync("Api2", this.ControllerContext.RouteData.Values["controller"].ToString(), this.ControllerContext.RouteData.Values["action"].ToString(), ex, DateTime.Now);
                var error = new ResponseResult<T>();
                error.Error = ex.ToString();
                return error;
            }

        }
        public class ResponseResult<T>
        {
            public T Result { get; set; }
            public string Error { get; set; }
        }
    }
}