﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Common;
using Core.Domain.Orderbook;
using Core.Services;
using Microsoft.Extensions.Caching.Distributed;

namespace LykkeApi2.Services
{
    public class OrderBooksService : IOrderBooksService
    {
        private readonly IDistributedCache _distributedCache;
        private readonly CacheSettings _cacheSettings;
        private readonly IAssetsHelper _assetsHelper;

        public OrderBooksService(
            IDistributedCache distributedCache,
            CacheSettings cacheSettings,
            IAssetsHelper assetsHelper)
        {
            _distributedCache = distributedCache;
            _cacheSettings = cacheSettings;
            _assetsHelper = assetsHelper;
        }
        public async Task<IEnumerable<IOrderBook>> GetAllAsync()
        {
            var assetPairs = await _assetsHelper.GetAllAssetPairsAsync();

            var orderBooks = new List<IOrderBook>();

            foreach (var pair in assetPairs)
            {
                var buyBookJson = _distributedCache.GetStringAsync(_cacheSettings.GetOrderBookKey(pair.Id, true));
                var sellBookJson = _distributedCache.GetStringAsync(_cacheSettings.GetOrderBookKey(pair.Id, false));

                var buyBook = (await buyBookJson)?.DeserializeJson<OrderBook>();
                if (buyBook != null)
                    orderBooks.Add(buyBook);

                var sellBook = (await sellBookJson)?.DeserializeJson<OrderBook>();
                if (sellBook != null)
                    orderBooks.Add(sellBook);
            }

            return orderBooks;
        }

        public async Task<IEnumerable<IOrderBook>> GetAsync(string assetPairId)
        {
            var sellBookTask = _distributedCache.GetStringAsync(_cacheSettings.GetOrderBookKey(assetPairId, false));
            var buyBookTask = _distributedCache.GetStringAsync(_cacheSettings.GetOrderBookKey(assetPairId, true));

            var sellBookStr = await sellBookTask;
            var sellBook = sellBookStr != null ? sellBookStr.DeserializeJson<OrderBook>() : 
                new OrderBook {AssetPair = assetPairId, IsBuy = false, Timestamp = DateTime.UtcNow};

            var buyBookStr = await buyBookTask;
            var buyBook = buyBookStr != null ? buyBookStr.DeserializeJson<OrderBook>() :
                new OrderBook { AssetPair = assetPairId, IsBuy = true, Timestamp = DateTime.UtcNow };

            return new IOrderBook[] { sellBook, buyBook };
        }
    }
}
