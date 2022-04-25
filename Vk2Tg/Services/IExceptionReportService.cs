namespace Vk2Tg.Services;

public interface IExceptionReportService
{
    Task SendExceptionAsync(Exception exception);
}
