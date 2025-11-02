using System;
using System.Threading.Tasks;

namespace FutureTechnologyE_Commerce.Services
{
    public interface ICacheService
    {
        Task<T?> GetAsync<T>(string key) where T : class;
        Task SetAsync<T>(string key, T value, TimeSpan? expiration = null) where T : class;
        Task RemoveAsync(string key);
        Task<bool> ExistsAsync(string key);
        Task RemoveByPrefixAsync(string prefix);
    }
}
