using System.Runtime.CompilerServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Vk2Tg.Abstractions.Services;
using VkNet.Abstractions;
using VkNet.Exception;
using VkNet.Model;
using VkNet.Model.GroupUpdate;

namespace Vk2Tg.Services;

public partial class VkLongPollService : IVkUpdateSourceService
{
    private readonly IVkApi _vkApi;
    private readonly ILogger<VkLongPollService> _logger;
    
    private string? _key;
    private string? _server;
    private string? _ts;
    private readonly ulong _groupId;

    public VkLongPollService(IConfiguration configuration, IVkApi vkApi, ILogger<VkLongPollService> logger)
    {
        _vkApi = vkApi;
        _logger = logger;
        _groupId = configuration.GetSection("vkGroupId").Get<ulong>();
    }
#region Logging
    [LoggerMessage(1, LogLevel.Error, "Long poll history request failed. Trying again. (error code: {ErrorCode}, message: {Message})")]
    partial void LogLpHistoryError(int errorCode, string message);
#endregion

    public async IAsyncEnumerable<GroupUpdate> GetGroupUpdatesAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        _logger.LogTrace("Getting long poll server...");
        var response = await _vkApi.Groups.GetLongPollServerAsync(_groupId);
        _key = response.Key;
        _server = response.Server;
        _ts = response.Ts;
        _logger.LogTrace("Got long poll server");

        _logger.LogTrace("Polling started");
        
        while (!cancellationToken.IsCancellationRequested)
        {
            var longPollResponse = await GetNextLongPollResponse();
            
            if (longPollResponse is null)
                continue;
            
            foreach (var groupUpdate in longPollResponse.Updates)
            {
                yield return groupUpdate;
            }
        }
    }
    
    private async Task<BotsLongPollHistoryResponse?> GetNextLongPollResponse()
    {
        BotsLongPollHistoryResponse longPollResponse;
        try
        {
            longPollResponse = await _vkApi.Groups.GetBotsLongPollHistoryAsync(new()
            {
                Key = _key,
                Server = _server,
                Ts = _ts,
                Wait = 15
            });
        }
        catch (LongPollOutdateException ex)
        {
            _logger.LogTrace("LongPollOutdateException: using new ts");
            _ts = ex.Ts;
            return null;
        }
        catch (LongPollKeyExpiredException)
        {
            _logger.LogTrace("LongPollKeyExpiredException: requesting key again");
            var longPollServerResponse = await _vkApi.Groups.GetLongPollServerAsync(_groupId);
            _key = longPollServerResponse.Key;
            return null;
        }
        catch (LongPollInfoLostException)
        {
            _logger.LogTrace("LongPollInfoLostException: requesting key and ts again");
            var longPollServerResponse = await _vkApi.Groups.GetLongPollServerAsync(_groupId);
            _key = longPollServerResponse.Key;
            _ts = longPollServerResponse.Ts;
            return null;
        }
        catch (VkApiMethodInvokeException apiEx) when (apiEx.Message.ToLowerInvariant().Contains("unknown application"))
        {
            LogLpHistoryError(apiEx.ErrorCode, apiEx.Message);
            throw;
        }
    
        _ts = longPollResponse.Ts;
    
        return longPollResponse;
    }
}
