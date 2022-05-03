using Microsoft.Extensions.Logging;
using Retell.Abstractions.Services;

namespace Retell.Services;

public class NullExceptionReportService : IExceptionReportService
{
    private readonly ILogger<NullExceptionReportService> _logger;
    public NullExceptionReportService(ILogger<NullExceptionReportService> logger)
    {
        _logger = logger;
    }
    
    public Task SendExceptionAsync(Exception exception)
    {
        _logger.LogDebug(exception, "Exception has been reported");
        return Task.CompletedTask;
    }
}
