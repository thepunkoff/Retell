namespace Vk2Tg.Abstractions.Services;

public interface IExceptionReportService
{
    Task SendExceptionAsync(Exception exception);
}
