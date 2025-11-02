using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace FutureTechnologyE_Commerce.Utility
{
    /// <summary>
    /// Performance monitoring utility for tracking execution times and database queries
    /// </summary>
    public class PerformanceMonitor : IDisposable
    {
        private readonly Stopwatch _stopwatch;
        private readonly ILogger _logger;
        private readonly string _operationName;
        private readonly LogLevel _logLevel;

        public PerformanceMonitor(ILogger logger, string operationName, LogLevel logLevel = LogLevel.Information)
        {
            _logger = logger;
            _operationName = operationName;
            _logLevel = logLevel;
            _stopwatch = Stopwatch.StartNew();
        }

        public void Dispose()
        {
            _stopwatch.Stop();
            var elapsedMs = _stopwatch.ElapsedMilliseconds;

            if (elapsedMs > 1000)
            {
                _logger.Log(LogLevel.Warning, 
                    "SLOW OPERATION: {OperationName} took {ElapsedMs}ms", 
                    _operationName, elapsedMs);
            }
            else
            {
                _logger.Log(_logLevel, 
                    "{OperationName} completed in {ElapsedMs}ms", 
                    _operationName, elapsedMs);
            }
        }

        public long ElapsedMilliseconds => _stopwatch.ElapsedMilliseconds;
    }

    /// <summary>
    /// Extension methods for performance monitoring
    /// </summary>
    public static class PerformanceMonitorExtensions
    {
        public static async Task<T> MonitorAsync<T>(
            this Task<T> task, 
            ILogger logger, 
            string operationName)
        {
            using var monitor = new PerformanceMonitor(logger, operationName);
            return await task;
        }

        public static T Monitor<T>(
            this Func<T> operation, 
            ILogger logger, 
            string operationName)
        {
            using var monitor = new PerformanceMonitor(logger, operationName);
            return operation();
        }
    }

    /// <summary>
    /// Cache performance metrics
    /// </summary>
    public class CacheMetrics
    {
        public long TotalRequests { get; set; }
        public long CacheHits { get; set; }
        public long CacheMisses { get; set; }
        public double HitRate => TotalRequests > 0 ? (double)CacheHits / TotalRequests * 100 : 0;
        public double MissRate => TotalRequests > 0 ? (double)CacheMisses / TotalRequests * 100 : 0;

        public void RecordHit()
        {
            TotalRequests++;
            CacheHits++;
        }

        public void RecordMiss()
        {
            TotalRequests++;
            CacheMisses++;
        }

        public override string ToString()
        {
            return $"Cache Stats - Total: {TotalRequests}, Hits: {CacheHits} ({HitRate:F2}%), Misses: {CacheMisses} ({MissRate:F2}%)";
        }
    }

    /// <summary>
    /// Database query performance tracker
    /// </summary>
    public class QueryPerformanceTracker
    {
        private readonly ILogger _logger;
        private int _queryCount;
        private long _totalQueryTime;

        public QueryPerformanceTracker(ILogger logger)
        {
            _logger = logger;
        }

        public void RecordQuery(long executionTimeMs)
        {
            _queryCount++;
            _totalQueryTime += executionTimeMs;

            if (executionTimeMs > 100)
            {
                _logger.LogWarning("Slow query detected: {ExecutionTime}ms", executionTimeMs);
            }
        }

        public void LogSummary()
        {
            if (_queryCount > 0)
            {
                var avgTime = _totalQueryTime / _queryCount;
                _logger.LogInformation(
                    "Query Summary - Count: {QueryCount}, Total Time: {TotalTime}ms, Avg Time: {AvgTime}ms",
                    _queryCount, _totalQueryTime, avgTime);

                if (_queryCount > 10)
                {
                    _logger.LogWarning("High query count detected: {QueryCount} queries executed", _queryCount);
                }
            }
        }
    }
}
