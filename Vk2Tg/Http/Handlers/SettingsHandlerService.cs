using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NLog;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace Vk2Tg.Http.Handlers;

public partial class SettingsHandlerService
{
    private readonly ILogger<SettingsHandlerService> _logger;
    private readonly IConfiguration _configuration;
    public SettingsHandlerService(ILogger<SettingsHandlerService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;

    }
#region Logging
    [LoggerMessage(1, LogLevel.Trace, "Invalid setting key: '{Key}'. Try 'enabled'")]
    partial void LogInvalidSettingsKey(string? key);
    [LoggerMessage(2, LogLevel.Trace, "Query string key '{Key}' was null or contained no values")]
    partial void LogQueryStringKeyIsNullOrEmpty(string? key);
    [LoggerMessage(3, LogLevel.Trace, "Value of query string key '{Key}' was null or empty")]
    partial void LogValueOfQueryStringKeyIsNullOrEmpty(string? key);
#endregion

    public async Task HandleSettingsRequest(HttpListenerContext context)
    {
        if (await Handle(context))
            _logger.LogTrace("Settings request processed. Ok");
        else
            _logger.LogTrace("Settings request processed. Bad Request");
    }
    
    private async Task<bool> Handle(HttpListenerContext context)
    {
        _logger.LogTrace("processing settings request...");

        var queryString = context.Request.QueryString;
        if (queryString.Keys.Count == 0)
        {
            await context.Response.ReturnBadRequest("Query string should contain at least one key.");
            return false;
        }
        
        for (var i = 0; i < queryString.Keys.Count; i++)
        {
            var key = queryString.Keys[i];
            if (key == "enabled")
            {
                var values = queryString.GetValues(i);
                if (values is null || values.Length == 0)
                {
                    LogQueryStringKeyIsNullOrEmpty(key);
                    await context.Response.ReturnBadRequest("Query string key '{key}' was null or contained no values.");
                    return false;
                }
        
                var value = values[0];
                if (string.IsNullOrWhiteSpace(value))
                {
                    LogValueOfQueryStringKeyIsNullOrEmpty(key);
                    await context.Response.ReturnBadRequest($"Value of query string key '{key}' was null or empty.");
                    return false;
                }

                var botEnabledSection = _configuration.GetSection("isBotEnabled");
                var botEnabled = botEnabledSection.Get<bool>();
                switch (value.ToLowerInvariant())
                {
                    case "true":
                        if (botEnabled)
                        {
                            _logger.LogInformation("Bot already enabled");
                        }
                        else
                        {
                            _logger.LogInformation("Enabling bot");
                            botEnabledSection.Value = "true";
                        }
                        break;
                    case "false":
                        if (botEnabled)
                        {
                            _logger.LogInformation("Disabling bot");
                            botEnabledSection.Value = "false";
                        }
                        else
                        {
                            _logger.LogInformation("Bot already disabled");
                        }
                        break;
                    default:
                        const string message = "Value of query string key 'enabled' could be either 'true' or 'false'.";
                        _logger.LogTrace(message);
                        await context.Response.ReturnBadRequest(message);
                        return false;
                }
            }
            else
            {
                LogInvalidSettingsKey(key);
                await context.Response.ReturnBadRequest($"Invalid setting key: '{key}'. Try 'enabled'.");
                return false;
            }
        }

        await context.Response.ReturnOk();
        return true;
    }
}