using Microsoft.AspNetCore.Mvc;
using NzbWebDAV.Models;
using NzbWebDAV.Services;

namespace NzbWebDAV.Api.Controllers.GetProviderStats;

[ApiController]
[Route("api/provider-stats")]
public class GetProviderStatsController(ProviderStatsService statsService) : BaseApiController
{
    protected override Task<IActionResult> HandleRequest()
    {
        var stats = statsService.GetCachedStats();

        if (stats == null)
        {
            return Task.FromResult<IActionResult>(Ok(new ProviderStatsResponse
            {
                Providers = new List<ProviderStats>(),
                TotalOperations = 0,
                CalculatedAt = DateTimeOffset.UtcNow,
                TimeWindow = TimeSpan.FromHours(24)
            }));
        }

        return Task.FromResult<IActionResult>(Ok(stats));
    }
}
