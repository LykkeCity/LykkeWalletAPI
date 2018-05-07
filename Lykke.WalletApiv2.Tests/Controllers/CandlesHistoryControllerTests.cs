// Copyright 2017 Lykke Corp.
// See LICENSE file in the project root for full license information.

namespace LykkeApi2.Tests.Controllers
{
    using System.Threading.Tasks;
    using Common;
    using Core.Candles;
    using FakeItEasy;
    using FluentAssertions;
    using Lykke.Service.Assets.Client;
    using Lykke.Service.Assets.Client.Models;
    using LykkeApi2.Controllers;
    using Microsoft.AspNetCore.Mvc;
    using Xbehave;

    public class CandlesHistoryControllerTests
    {
        [Scenario(Skip = "TODO (Rachael) CachedDataDictionary cannot be faked")]
        public void Do(CandlesHistoryController candlesHistoryController, Task<IActionResult> result)
        {
            "Given a candles history controller"
                .x(() =>
                {
                    candlesHistoryController = new CandlesHistoryController(
                        A.Fake<ICandlesHistoryServiceProvider>(),
                        A.Fake<IAssetsService>(),
                        A.Fake<CachedDataDictionary<string, AssetPair>>());
                });

            "When"
                .x(() =>
                {
                    result = candlesHistoryController.GetCandles(null);
                });

            "Then the result is OK and returns the expected model"
                .x(() =>
                {
                    result.Result.Should().BeOfType(typeof(OkObjectResult));
                });
        }
    }
}
