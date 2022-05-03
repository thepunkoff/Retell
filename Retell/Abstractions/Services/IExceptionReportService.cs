namespace Retell.Abstractions.Services;

public interface IExceptionReportService
{
    Task SendExceptionAsync(Exception exception);
}
