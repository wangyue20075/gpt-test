using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace Oc.BinGrid.Core.Logging
{
    public static class LogThrottler
    {
        private static readonly ConcurrentDictionary<string, DateTime> _lastLogTimes = new();

        /// <summary>
        /// 判断是否可以打印日志
        /// </summary>
        /// <param name="key">限流标识（建议格式：模块:行为:唯一ID）</param>
        /// <param name="interval">间隔时间</param>
        public static bool ShouldLog(string key, TimeSpan interval)
        {
            var now = DateTime.UtcNow;
            var lastLog = _lastLogTimes.GetOrAdd(key, DateTime.MinValue);

            if (now - lastLog < interval) return false;

            // 只有真正更新时才写字典，减少锁竞争
            _lastLogTimes[key] = now;
            return true;
        }

        /// <summary>
        /// 扩展方法：带限流功能的日志输出
        /// </summary>
        public static void LogLimited(this ILogger logger, LogLevel level, string key, TimeSpan interval, string message, params object?[] args)
        {
            if (ShouldLog(key, interval))
            {
                logger.Log(level, message, args);
            }
        }

        /// <summary>
        /// 清理过期的限流缓存（建议每天执行一次或在内存压力大时执行）
        /// </summary>
        public static void ClearExpired(TimeSpan maxAge)
        {
            var now = DateTime.UtcNow;
            var expiredKeys = _lastLogTimes.Where(kv => now - kv.Value > maxAge).Select(kv => kv.Key);
            foreach (var key in expiredKeys) _lastLogTimes.TryRemove(key, out _);
        }
    }
}
