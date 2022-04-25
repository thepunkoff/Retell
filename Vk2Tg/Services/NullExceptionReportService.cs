namespace Vk2Tg.Services;

public class NullExceptionReportService : IExceptionReportService
{
    public Task SendExceptionAsync(Exception exception)
    {
        return Task.CompletedTask;
    }
}
