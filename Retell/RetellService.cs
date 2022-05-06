using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Retell.Abstractions.Services;
using Retell.Core;
using Retell.Filtering;

namespace Retell;

public class RetellService : BackgroundService
{
    private readonly ILogger<RetellService> _logger;
    private readonly IEnumerable<IPostSource> _sources;
    private readonly IEnumerable<IPostRenderer> _renderers;
    private readonly IPostFilteringService _postFilteringService;
    private readonly IExceptionReportService _reportService;
    public RetellService(ILogger<RetellService> logger, 
        IEnumerable<IPostSource> sources, 
        IEnumerable<IPostRenderer> renderers,
        IPostFilteringService postFilteringService,
        IExceptionReportService reportService)
    {
        _logger = logger;
        _sources = sources;
        _renderers = renderers;
        _postFilteringService = postFilteringService;
        _reportService = reportService;
    }

    protected override async Task ExecuteAsync(CancellationToken token)
    {
        _logger.LogTrace("Starting bot service...");
        while (!token.IsCancellationRequested)
        {
            try
            {
                await foreach (var post in _sources.Select(source => source.GetPosts(token))
                                   .Merge()
                                   .Where(post => _postFilteringService.Filter(post) is FilteringResult.ShouldShow)
                                   .WithCancellation(token))
                {
                    await Task.WhenAll(_renderers.Select(renderer => renderer.RenderAsync(post, token)));
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Exception occurred. Continuing...");
                await _reportService.SendExceptionAsync(e);
            }
        }
    }
}