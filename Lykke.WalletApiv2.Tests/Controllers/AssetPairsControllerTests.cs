// Copyright 2017 Lykke Corp.
// See LICENSE file in the project root for full license information.

namespace LykkeApi2.Tests.Controllers
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Common;
    using FakeItEasy;
    using FluentAssertions;
    using Lykke.MarketProfileService.Client;
    using Lykke.Service.Assets.Client;
    using Lykke.Service.Assets.Client.Models;
    using LykkeApi2.Controllers;
    using LykkeApi2.Infrastructure;
    using Microsoft.AspNetCore.Mvc;
    using Xbehave;

    public class AssetPairsControllerTests
    {
        [Scenario(Skip = "TODO (Rachael) CachedDataDictionary cannot be faked")]
        public void GetExistingLink(
            CachedDataDictionary<string, AssetPair> assetPairsCache,
            CachedDataDictionary<string, Asset> assetsCache,
            IRequestContext requestContext,
            AssetPairsController assetPairsController, 
            Task<IActionResult> result)
        {
            "Given an asset pairs cache"
                .x(() =>
                {
                    assetPairsCache = A.Fake<CachedDataDictionary<string, AssetPair>>();
                    IEnumerable<AssetPair> assetPairs = null;
                    A.CallTo(() => assetPairsCache.Values()).Returns(Task.FromResult(assetPairs));
                });

            "And an asset cache"
                .x(() =>
                {
                    assetsCache = A.Fake<CachedDataDictionary<string, Asset>>();
                    IEnumerable<Asset> assets = null;
                    A.CallTo(() => assetsCache.Values()).Returns(Task.FromResult(assets));
                });

            "And a request context"
                .x(() =>
                {
                    requestContext = A.Fake<IRequestContext>();
                    A.CallTo(() => requestContext.ClientId).Returns("clientId");
                });

            "And an asset pairs controller"
                .x(() =>
                {
                    assetPairsController = new AssetPairsController(
                        assetPairsCache, 
                        assetsCache, 
                        A.Fake<IAssetsService>(),
                        A.Fake<ILykkeMarketProfileServiceAPI>(),
                        requestContext);
                });

            "When I request all asset pairs"
                .x(() =>
                {
                    result = assetPairsController.Get();
                });

            "Then the result is OK and returns the expected model"
                .x(() =>
                {
                    result.Result.Should().BeOfType(typeof(OkObjectResult));
                    ((OkObjectResult)result.Result).Value.Should().BeOfType(typeof(Models.AssetPairsModels.AssetPairResponseModel));
                    ((Models.AssetPairsModels.AssetPairResponseModel)((OkObjectResult)result.Result).Value).AssetPairs.Count().Should().Be(0);
                });
        }
    }
}
