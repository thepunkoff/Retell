using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Vk2Tg.Abstractions.Services;
using Vk2Tg.Core;

namespace Vk2Tg;

public class BotService : BackgroundService
{
    private readonly ILogger<BotService> _logger;
    private readonly IPostSource _source;
    private readonly IPostRenderer _renderer;
    private readonly IExceptionReportService _reportService;
    public BotService(ILogger<BotService> logger, IPostSource source, IPostRenderer renderer, IExceptionReportService reportService)
    {
        _logger = logger;
        _source = source;
        _renderer = renderer;
        _reportService = reportService;
    }

    protected override async Task ExecuteAsync(CancellationToken token)
    {
        _logger.LogTrace("Starting bot service...");
        while (!token.IsCancellationRequested)
        {
            try
            {
                await foreach (var post in _source.GetPosts(token))
                    await _renderer.Render(post, token);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Exception occurred. Continuing...");
                await _reportService.SendExceptionAsync(e);
            }
        }
    }
}