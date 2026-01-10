using Microsoft.EntityFrameworkCore;
using NzbWebDAV.Database;
using NzbWebDAV.Models;
using NzbWebDAV.Utils;
using Serilog;

namespace NzbWebDAV.Services;

/// <summary>
/// Background service that periodically calculates and caches provider usage statistics
/// </summary>
public class ProviderStatsService
{
    private readonly CancellationToken _cancellationToken = SigtermUtil.GetCancellationToken();
    private ProviderStatsResponse? _cachedStats;
    private readonly object _lock = new();

    public ProviderStatsService()
    {
        _ = StartStatsCalculationLoop();
    }

    /// <summary>
    /// Gets the cached provider statistics
    /// </summary>
    public ProviderStatsResponse? GetCachedStats()
    {
        lock (_lock)
        {
            return _cachedStats;
        }
    }

    private async Task StartStatsCalculationLoop()
    {
        // Calculate immediately on startup
        await CalculateAndCacheStats().ConfigureAwait(false);

        while (!_cancellationToken.IsCancellationRequested)
        {
            try
            {
                // Wait 15 minutes before next calculation
                await Task.Delay(TimeSpan.FromMinutes(15), _cancellationToken).ConfigureAwait(false);

                await CalculateAndCacheStats().ConfigureAwait(false);
            }
            catch (Exception ex) when (!_cancellationToken.IsCancellationRequested)
            {
                Log.Error(ex, "Error in provider stats calculation loop");
                await Task.Delay(TimeSpan.FromMinutes(1), _cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private async Task CalculateAndCacheStats()
    {
        try
        {
            var timeWindow = TimeSpan.FromHours(24);
            var startTime = DateTimeOffset.UtcNow.Add(-timeWindow);

            await using var dbContext = new DavDatabaseContext();

            // Query events from last 24 hours with operation type
            var events = await dbContext.ProviderUsageEvents
                .Where(e => e.CreatedAt >= startTime && e.OperationType != null)
                .Select(e => new { e.ProviderHost, e.ProviderType, e.OperationType })
                .ToListAsync(_cancellationToken)
                .ConfigureAwait(false);

            if (events.Count == 0)
            {
                lock (_lock)
                {
                    _cachedStats = new ProviderStatsResponse
                    {
                        Providers = new List<ProviderStats>(),
                        TotalOperations = 0,
                        CalculatedAt = DateTimeOffset.UtcNow,
                        TimeWindow = timeWindow
                    };
                }
                return;
            }

            // Group by provider and calculate stats
            var providerGroups = events
                .GroupBy(e => new { e.ProviderHost, e.ProviderType })
                .Select(g => new
                {
                    g.Key.ProviderHost,
                    g.Key.ProviderType,
                    TotalOperations = (long)g.Count(),
                    OperationCounts = g.GroupBy(e => e.OperationType!)
                        .ToDictionary(og => og.Key, og => (long)og.Count())
                })
                .ToList();

            var totalOperations = (long)events.Count;

            var providerStats = providerGroups
                .Select(pg => new ProviderStats
                {
                    ProviderHost = pg.ProviderHost,
                    ProviderType = pg.ProviderType,
                    TotalOperations = pg.TotalOperations,
                    OperationCounts = pg.OperationCounts,
                    PercentageOfTotal = totalOperations > 0
                        ? Math.Round((double)pg.TotalOperations / totalOperations * 100, 1)
                        : 0
                })
                .OrderByDescending(ps => ps.TotalOperations)
                .ToList();

            lock (_lock)
            {
                _cachedStats = new ProviderStatsResponse
                {
                    Providers = providerStats,
                    TotalOperations = totalOperations,
                    CalculatedAt = DateTimeOffset.UtcNow,
                    TimeWindow = timeWindow
                };
            }

            Log.Debug("Provider stats calculated: {TotalOperations} operations across {ProviderCount} providers",
                totalOperations, providerStats.Count);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to calculate provider stats");
        }
    }
}
