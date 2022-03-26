using System.Collections.Specialized;
using System.Net;
using NLog;

namespace Vk2Tg.Http.Handlers;

public class SettingsHandler
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public async Task HandleSettingsRequest(HttpListenerContext context)
    {
        if (await Handle(context))
            Logger.Trace($"[{nameof(SettingsHandler)}] Settings request processed. Ok.");
        else
            Logger.Trace($"[{nameof(SettingsHandler)}] Settings request processed. Bad Request.");
    }
    
    private async Task<bool> Handle(HttpListenerContext context)
    {
        Logger.Trace($"[{nameof(SettingsHandler)}] processing settings request...");

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
                    var message = $"Query string key '{key}' was null or contained no values.";
                    Logger.Trace(message);
                    await context.Response.ReturnBadRequest(message);
                    return false;
                }
        
                var value = values[0];
                if (string.IsNullOrWhiteSpace(value))
                {
                    var message = $"Value of query string key '{key}' was null or empty.";
                    Logger.Trace(message);
                    await context.Response.ReturnBadRequest(message);
                    return false;
                }

                switch (value.ToLowerInvariant())
                {
                    case "true":
                        if (DynamicSettings.IsBotEnabled)
                        {
                            Logger.Trace($"[{nameof(SettingsHandler)}] Bot already enabled.");
                        }
                        else
                        {
                            Logger.Info($"[{nameof(SettingsHandler)}] Enabling bot.");
                            DynamicSettings.IsBotEnabled = true;
                        }
                        break;
                    case "false":
                        if (!DynamicSettings.IsBotEnabled)
                        {
                            Logger.Trace($"[{nameof(SettingsHandler)}] Bot already disabled.");
                        }
                        else
                        {
                            Logger.Info($"[{nameof(SettingsHandler)}] Disabling bot.");
                            DynamicSettings.IsBotEnabled = false;
                        }
                        break;
                    default:
                        const string message = "Value of query string key 'enabled' could be either 'true' or 'false'.";
                        Logger.Trace(message);
                        await context.Response.ReturnBadRequest(message);
                        return false;
                }
            }
            else
            {
                var message = $"Invalid setting key: '{key}'. Try 'enabled'.";
                Logger.Trace(message);
                await context.Response.ReturnBadRequest(message);
                return false;
            }
        }

        await context.Response.ReturnOk();
        return true;
    }
}