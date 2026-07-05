using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace Application.Cache
{
    /// <summary>
    /// Redis 缓存服务 — 提供统一的缓存读写方法
    /// </summary>
    public interface IRedisCacheService
    {
        /// <summary>
        /// 从 Redis 获取缓存（返回 nullable）
        /// </summary>
        T? GetNullable<T>(string key);

        /// <summary>
        /// 从 Redis 获取缓存
        /// </summary>
        T Get<T>(string key);

        /// <summary>
        /// 向 Redis 设置缓存（默认过期时间 30 分钟）
        /// </summary>
        void Set<T>(string key, T value, TimeSpan? expiresIn = null);

        /// <summary>
        /// 从 Redis 移除缓存
        /// </summary>
        void Remove(string key);

        /// <summary>
        /// 异步获取缓存
        /// </summary>
        Task<T> GetAsync<T>(string key);

        /// <summary>
        /// 异步设置缓存
        /// </summary>
        Task SetAsync<T>(string key, T value, TimeSpan? expiresIn = null);
    }

    public class RedisCacheService : IRedisCacheService
    {
        private readonly IDistributedCache _cache;
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public RedisCacheService(IDistributedCache cache)
        {
            _cache = cache;
        }

        public T? GetNullable<T>(string key)
        {
            var json = _cache.GetString(key);
            if (string.IsNullOrEmpty(json)) return default;
            return JsonSerializer.Deserialize<T>(json, _jsonOptions);
        }

        public T Get<T>(string key)
        {
            var json = _cache.GetString(key);
            if (string.IsNullOrEmpty(json)) return default!;
            return JsonSerializer.Deserialize<T>(json, _jsonOptions) ?? default!;
        }

        public void Set<T>(string key, T value, TimeSpan? expiresIn = null)
        {
            var json = JsonSerializer.Serialize(value);
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiresIn ?? TimeSpan.FromMinutes(30)
            };
            _cache.SetString(key, json, options);
        }

        public void Remove(string key)
        {
            _cache.Remove(key);
        }

        public async Task<T> GetAsync<T>(string key)
        {
            var json = await _cache.GetStringAsync(key);
            if (string.IsNullOrEmpty(json)) return default!;
            return JsonSerializer.Deserialize<T>(json, _jsonOptions) ?? default!;
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? expiresIn = null)
        {
            var json = JsonSerializer.Serialize(value);
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiresIn ?? TimeSpan.FromMinutes(30)
            };
            await _cache.SetStringAsync(key, json, options);
        }
    }
}
