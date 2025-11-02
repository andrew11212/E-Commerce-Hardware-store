using Microsoft.Extensions.Caching.Distributed;
using System;
using System.Text.Json;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace FutureTechnologyE_Commerce.Services
{
    public class RedisCacheService : ICacheService
    {
        private readonly IDistributedCache _distributedCache;
        private readonly IConnectionMultiplexer _redis;
        private readonly ILogger<RedisCacheService> _logger;
        
        // JSON serializer options to handle circular references
        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles,
            WriteIndented = false,
            PropertyNamingPolicy = null,
            MaxDepth = 64 // Prevent deep object graphs
        };

        public RedisCacheService(
            IDistributedCache distributedCache, 
            IConnectionMultiplexer redis,
            ILogger<RedisCacheService> logger)
        {
            _distributedCache = distributedCache;
            _redis = redis;
            _logger = logger;
        }

        public async Task<T?> GetAsync<T>(string key) where T : class
        {
            try
            {
                var cachedData = await _distributedCache.GetStringAsync(key);
                
                if (string.IsNullOrEmpty(cachedData))
                    return null;

                return JsonSerializer.Deserialize<T>(cachedData, _jsonOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving data from cache for key: {Key}", key);
                return null;
            }
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null) where T : class
        {
            try
            {
                var serializedData = JsonSerializer.Serialize(value, _jsonOptions);
                
                var options = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = expiration ?? TimeSpan.FromMinutes(30)
                };

                await _distributedCache.SetStringAsync(key, serializedData, options);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting cache for key: {Key}", key);
            }
        }

        public async Task RemoveAsync(string key)
        {
            try
            {
                await _distributedCache.RemoveAsync(key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing cache for key: {Key}", key);
            }
        }

        public async Task<bool> ExistsAsync(string key)
        {
            try
            {
                var cachedData = await _distributedCache.GetStringAsync(key);
                return !string.IsNullOrEmpty(cachedData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking cache existence for key: {Key}", key);
                return false;
            }
        }

        public async Task RemoveByPrefixAsync(string prefix)
        {
            try
            {
                var server = _redis.GetServer(_redis.GetEndPoints().First());
                var keys = server.Keys(pattern: $"{prefix}*");
                
                foreach (var key in keys)
                {
                    await _distributedCache.RemoveAsync(key.ToString());
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing cache by prefix: {Prefix}", prefix);
            }
        }
    }
}
