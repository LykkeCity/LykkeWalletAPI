using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;

namespace LkeServices.Identity
{
    public class ClaimsCache
    {
        public class PrincipalCashItem
        {
            public ClaimsPrincipal ClaimsPrincipal { get; set; }
            public DateTime LastRefresh { get; private set; }

            public static PrincipalCashItem Create(ClaimsPrincipal src)
            {
                return new PrincipalCashItem
                {
                    LastRefresh = DateTime.UtcNow,
                    ClaimsPrincipal = src
                };
            }
        }

        private readonly int _secondsToExpire;

        private readonly Dictionary<string, PrincipalCashItem> _claimsCache = new Dictionary<string, PrincipalCashItem>();
        private readonly ReaderWriterLockSlim _cacheLock = new ReaderWriterLockSlim();

        public ClaimsCache(int secondsToExpire = 60)
        {
            _secondsToExpire = secondsToExpire;
        }

        public ClaimsPrincipal Get(string token)
        {
            _cacheLock.EnterUpgradeableReadLock();

            try
            {
                if (!_claimsCache.TryGetValue(token, out PrincipalCashItem result))
                {
                    return null;
                }

                if ((DateTime.UtcNow - result.LastRefresh).TotalSeconds < _secondsToExpire)
                    return result.ClaimsPrincipal;

                _cacheLock.EnterWriteLock();

                try
                {
                    _claimsCache.Remove(token);

                    return null;
                }
                finally
                {
                    _cacheLock.ExitWriteLock();
                }
            }
            finally
            {
                _cacheLock.ExitUpgradeableReadLock();
            }
        }

        public void Invalidate(string token)
        {
            lock (_claimsCache)
            {
                _claimsCache.Remove(token);
            }
        }

        public void Set(string token, ClaimsPrincipal principal)
        {
            _cacheLock.EnterWriteLock();

            try
            {
                if (_claimsCache.ContainsKey(token))
                    _claimsCache[token] = PrincipalCashItem.Create(principal);
                else
                    _claimsCache.Add(token, PrincipalCashItem.Create(principal));
            }
            finally
            {
                _cacheLock.ExitWriteLock();
            }
        }
    }
}