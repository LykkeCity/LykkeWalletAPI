// Copyright 2017 Lykke Corp.
// See LICENSE file in the project root for full license information.

namespace LykkeApi2.Tests.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Common.Log;
    using Core.Constants;
    using FakeItEasy;
    using FluentAssertions;
    using Lykke.Service.Affiliate.Client;
    using Lykke.Service.Affiliate.Contracts;
    using Lykke.Service.RateCalculator.Client;
    using Lykke.Service.RateCalculator.Client.AutorestClient.Models;
    using LykkeApi2.Controllers;
    using LykkeApi2.Infrastructure;
    using LykkeApi2.Models.Affiliate;
    using Microsoft.AspNetCore.Mvc;
    using Xbehave;

    public class AffiliateControllerTests
    {
        [Scenario]
        public void GetExistingLink(IAffiliateClient affiliateClient, IRequestContext requestContext, AffiliateController affiliateController, Task<IActionResult> result)
        {
            "Given an affiliateClient with a link"
                .x(() =>
                {
                    affiliateClient = A.Fake<IAffiliateClient>();
                    IEnumerable<LinkModel> list = new List<LinkModel> { new LinkModel { Url = "url", RedirectUrl = "redirectUrl" } };
                    A.CallTo(() => affiliateClient.GetLinks("clientId")).Returns(Task.FromResult(list));
                });

            "And a request context"
                .x(() =>
                {
                    requestContext = A.Fake<IRequestContext>();
                    A.CallTo(() => requestContext.ClientId).Returns("clientId");
                });

            "And an affiliate controller"
                .x(() =>
                {
                    affiliateController = new AffiliateController(affiliateClient, requestContext, A.Fake<IRateCalculatorClient>(), A.Fake<ILog>());
                });

            "When I get a link that exists"
                .x(() =>
                {
                    result = affiliateController.GetLink();
                });

            "Then the result is OK and returns the expected model"
                .x(() =>
                {
                    result.Result.Should().BeOfType(typeof(OkObjectResult));
                    ((OkObjectResult)result.Result).Value.Should().BeOfType(typeof(AffiliateLinkResponse));
                    ((AffiliateLinkResponse)((OkObjectResult)result.Result).Value).Url.Should().Be("url");
                    ((AffiliateLinkResponse)((OkObjectResult)result.Result).Value).RedirectUrl.Should().Be("redirectUrl");
                });
        }


        [Scenario]
        public void GetNonExistingLink(IAffiliateClient affiliateClient, IRequestContext requestContext, AffiliateController affiliateController, Task<IActionResult> result)
        {
            "Given an affiliateClient with no links"
                .x(() =>
                {
                    affiliateClient = A.Fake<IAffiliateClient>();
                    IEnumerable<LinkModel> list = new List<LinkModel>();
                    A.CallTo(() => affiliateClient.GetLinks("clientId")).Returns(Task.FromResult(list));
                });

            "And a request context"
                .x(() =>
                {
                    requestContext = A.Fake<IRequestContext>();
                    A.CallTo(() => requestContext.ClientId).Returns("clientId");
                });

            "And an affiliate controller"
                .x(() =>
                {
                    affiliateController = new AffiliateController(affiliateClient, requestContext, A.Fake<IRateCalculatorClient>(), A.Fake<ILog>());
                });

            "When I get a link that does not exists"
                .x(() =>
                {
                    result = affiliateController.GetLink();
                });

            "Then the result is NotFound"
                .x(() =>
                {
                    result.Result.Should().BeOfType(typeof(NotFoundResult));
                });
        }

        [Scenario]
        public void CreateLink(IAffiliateClient affiliateClient, IRequestContext requestContext, AffiliateController affiliateController, Task<IActionResult> result)
        {
            "Given an affiliateClient"
                .x(() =>
                {
                    affiliateClient = A.Fake<IAffiliateClient>();
                    var model = new LinkModel { Url = "url", RedirectUrl = "redirectUrl" };
                    A.CallTo(() => affiliateClient.RegisterLink("clientId", "https://lykke.com")).Returns(Task.FromResult(model));
                });

            "And a request context"
                .x(() =>
                {
                    requestContext = A.Fake<IRequestContext>();
                    A.CallTo(() => requestContext.ClientId).Returns("clientId");
                });

            "And an affiliate controller"
                .x(() =>
                {
                    affiliateController = new AffiliateController(affiliateClient, requestContext, A.Fake<IRateCalculatorClient>(), A.Fake<ILog>());
                });

            "When I create a link"
                .x(() =>
                {
                    result = affiliateController.CreateLink();
                });

            "Then the result is OK and returns the expected model"
                .x(() =>
                {
                    result.Result.Should().BeOfType(typeof(OkObjectResult));
                    ((AffiliateLinkResponse)((OkObjectResult)result.Result).Value).Url.Should().Be("url");
                    ((AffiliateLinkResponse)((OkObjectResult)result.Result).Value).RedirectUrl.Should().Be("redirectUrl");

                });
        }

        [Scenario]
        public void GetStatsForBitcoin(IAffiliateClient affiliateClient, IRequestContext requestContext, AffiliateController affiliateController, Task<IActionResult> result)
        {
            "Given an affiliateClient"
                .x(() =>
                {
                    affiliateClient = A.Fake<IAffiliateClient>();

                    IEnumerable<ReferralModel> referrals = new List<ReferralModel> { new ReferralModel { Id = "id", CreatedDt = new DateTime(18, 1, 1) } };
                    A.CallTo(() => affiliateClient.GetReferrals("clientId")).Returns(Task.FromResult(referrals));

                    IEnumerable<StatisticItemModel> stats = new List<StatisticItemModel>
                    {
                        new StatisticItemModel { AssetId = LykkeConstants.BitcoinAssetId, TradeVolume = 18, BonusVolume = 10 },
                    };
                    A.CallTo(() => affiliateClient.GetStats("clientId")).Returns(Task.FromResult(stats));
                });

            "And a request context"
                .x(() =>
                {
                    requestContext = A.Fake<IRequestContext>();
                    A.CallTo(() => requestContext.ClientId).Returns("clientId");
                });

            "And an affiliate controller"
                .x(() =>
                {
                    affiliateController = new AffiliateController(affiliateClient, requestContext, A.Fake<IRateCalculatorClient>(), A.Fake<ILog>());
                });

            "When I get statistics"
                .x(() =>
                {
                    result = affiliateController.GetStats();
                });

            "Then the result is OK and returns the expected model"
                .x(() =>
                {
                    result.Result.Should().BeOfType(typeof(OkObjectResult));
                    ((AffiliateStatisticsResponse)((OkObjectResult)result.Result).Value).ReferralsCount.Should().Be(1);
                    ((AffiliateStatisticsResponse)((OkObjectResult)result.Result).Value).TotalBonus.Should().Be(10);
                    ((AffiliateStatisticsResponse)((OkObjectResult)result.Result).Value).TotalTradeVolume.Should().Be(18);
                });
        }

        [Scenario]
        public void GetStatsForBitcoinAndOtherAssets(IAffiliateClient affiliateClient, IRequestContext requestContext, IRateCalculatorClient rateCalculatorClient, AffiliateController affiliateController, Task<IActionResult> result)
        {
            "Given an affiliateClient"
                .x(() =>
                {
                    affiliateClient = A.Fake<IAffiliateClient>();

                    IEnumerable<ReferralModel> referrals = new List<ReferralModel> { new ReferralModel { Id = "id", CreatedDt = new DateTime(18, 1, 1) } };
                    A.CallTo(() => affiliateClient.GetReferrals("clientId")).Returns(Task.FromResult(referrals));

                    IEnumerable<StatisticItemModel> stats = new List<StatisticItemModel>
                    {
                        new StatisticItemModel { AssetId = LykkeConstants.BitcoinAssetId, TradeVolume = 18, BonusVolume = 10 },
                        new StatisticItemModel { AssetId = LykkeConstants.EthAssetId, TradeVolume = 32, BonusVolume = 5 },
                    };
                    A.CallTo(() => affiliateClient.GetStats("clientId")).Returns(Task.FromResult(stats));
                });

            "And a request context"
                .x(() =>
                {
                    requestContext = A.Fake<IRequestContext>();
                    A.CallTo(() => requestContext.ClientId).Returns("clientId");
                });

            "And a rate calculator client"
                .x(() =>
                {
                    rateCalculatorClient = A.Fake<IRateCalculatorClient>();
                    A.CallTo(() => requestContext.ClientId).Returns("clientId");
                });

            "And an affiliate controller"
                .x(() =>
                {
                    affiliateController = new AffiliateController(affiliateClient, requestContext, rateCalculatorClient, A.Fake<ILog>());

                    // Trade Volume
                    IEnumerable<ConversionResult> conversionResult = new List<ConversionResult> { new ConversionResult(1, 23, OperationResult.Ok, new AssetWithAmount(32, LykkeConstants.EthAssetId ), new AssetWithAmount(32, LykkeConstants.BitcoinAssetId)) };
                    A.CallTo(() => rateCalculatorClient.GetMarketAmountInBaseAsync(
                        A<IEnumerable<AssetWithAmount>>.That.Matches(enumerable => enumerable.First().Amount == 32),
                        A<string>.Ignored, 
                        A<OrderAction>.Ignored)).Returns(Task.FromResult(conversionResult));

                    // BonusVolume
                    IEnumerable<ConversionResult> conversionResult2 = new List<ConversionResult> { new ConversionResult(1, 5, OperationResult.Ok, new AssetWithAmount(5, LykkeConstants.EthAssetId), new AssetWithAmount(5, LykkeConstants.BitcoinAssetId))};
                    A.CallTo(() => rateCalculatorClient.GetMarketAmountInBaseAsync(
                        A<IEnumerable<AssetWithAmount>>.That.Matches(enumerable => enumerable.First().Amount == 5),
                        A<string>.Ignored,
                        A<OrderAction>.Ignored)).Returns(Task.FromResult(conversionResult2));
                });

            "When I get statistics"
                .x(() =>
                {
                    result = affiliateController.GetStats();
                });

            "Then the result is OK and returns the expected model"
                .x(() =>
                {
                    result.Result.Should().BeOfType(typeof(OkObjectResult));
                    ((AffiliateStatisticsResponse)((OkObjectResult)result.Result).Value).ReferralsCount.Should().Be(1);
                    ((AffiliateStatisticsResponse)((OkObjectResult)result.Result).Value).TotalBonus.Should().Be(15);
                    ((AffiliateStatisticsResponse)((OkObjectResult)result.Result).Value).TotalTradeVolume.Should().Be(50);
                });
        }
    }
}
