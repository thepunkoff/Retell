using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Retell.Abstractions.Services;
using Retell.Configuration;
using Retell.Core;
using Retell.Core.Models;
using VkNet.Abstractions;
using VkNet.Enums.Filters;
using VkNet.Exception;
using VkNet.Model;
using VkNet.Model.Attachments;
using VkNet.Model.GroupUpdate;
using VkNet.Model.RequestParams;

namespace Retell.Vk;

/// <summary>
/// Post source implementation for vk.com.
/// </summary>
public class VkPostSource : IPostSource
{
    private readonly IVkApi _vkApi;
    private readonly IExceptionReportService _reportService;
    private readonly ulong _groupId;
    private readonly VkSecrets _vkSecrets;
    private readonly ILogger<VkPostSource> _logger;

    private string? _key;
    private string? _server;
    private string? _ts;

    /// <summary>
    /// Post source implementation for vk.com.
    /// </summary>
    public VkPostSource(IConfiguration configuration, IVkApi vkApi, IExceptionReportService reportService, ILogger<VkPostSource> logger)
    {
        _vkApi = vkApi;
        _reportService = reportService;
        _logger = logger;
        _groupId = configuration.GetSection("vkGroupId").Get<ulong>();
        _vkSecrets = configuration.Get<VkSecrets>();
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<Core.Models.Post> GetPosts([EnumeratorCancellation] CancellationToken token)
    {

        await _vkApi.AuthorizeAsync(new ApiAuthParams
        {
            AccessToken = _vkSecrets.VkToken,
            Settings = Settings.All | Settings.Offline,
            TwoFactorAuthorization = Console.ReadLine,
        });

        _logger.LogTrace("Getting long poll server...");
        try
        {
            var response = await _vkApi.Groups.GetLongPollServerAsync(_groupId);
            _key = response.Key;
            _server = response.Server;
            _ts = response.Ts;
        }
        catch (UserAuthorizationFailException ex)
        {
            await _reportService.SendExceptionAsync(ex);
            Environment.FailFast("Vk authorization error.", ex);
        }
        _logger.LogTrace("Got long poll server");

        _logger.LogTrace("Polling started");

        while (!token.IsCancellationRequested)
        {
            var longPollResponse = await GetNextLongPollResponse();

            if (longPollResponse is null)
                continue;

            foreach (var groupUpdate in longPollResponse.Updates)
                if (TryMapToDomainPost(groupUpdate.WallPost, out var post))
                    yield return post;
        }
    }

    private bool TryMapToDomainPost(WallPost? vkPost, [NotNullWhen(true)] out Core.Models.Post? post)
    {
        post = null;

        if (vkPost is null)
            return false;

        if (vkPost.FromId is null)
        {
            _logger.LogWarning("No FromId in the wall post");
            return false;
        }

        if (vkPost.FromId != -(long)_groupId)
            return false;

        var text = vkPost.Text;
        var media = new List<Medium>();
        Core.Models.Poll? poll = null;
        var links = new List<Uri>();
        foreach (var attachment in vkPost.Attachments)
        {
            switch (attachment.Instance)
            {
                case Photo photo:
                    media.Add(new Medium(MediumType.Photo, photo.Sizes.Last().Url));
                    break;
                case Video video:
                    // TODO: error handling
                    var videoCollection = _vkApi.Video.Get(new VideoGetParams { Videos = new[] { video } });
                    var bestQualityUri = ChoseBestQualityUrl(videoCollection[0]);
                    media.Add(new Medium(MediumType.Video, bestQualityUri));
                    break;
                case Document { Ext: "gif" } gif:
                    media.Add(new Medium(MediumType.Gif, new Uri(gif.Uri)));
                    break;
                case VkNet.Model.Attachments.Poll vkPoll:
                    poll = new Core.Models.Poll(vkPoll.Question, vkPoll.Answers.Select(x => x.Text).ToArray(), vkPoll.Multiple ?? false);
                    break;
                case Link vkLink:
                    links.Add(vkLink.Uri);
                    break;
            }
        }

        post = new Core.Models.Post(text, media.Count > 0 ? media.ToArray() : null, poll, links.Count > 0 ? links.ToArray() : null);
        return true;
    }

    private static Uri ChoseBestQualityUrl(Video video)
    {
        return video.Files switch
        {
            { External: { } } => video.Files.External,
            { Mp4_1080: { } } => video.Files.Mp4_1080,
            { Mp4_720: { } } => video.Files.Mp4_720,
            { Mp4_480: { } } => video.Files.Mp4_480,
            { Mp4_360: { } } => video.Files.Mp4_360,
            { Mp4_240: { } } => video.Files.Mp4_240,
            _ => throw new ArgumentOutOfRangeException(nameof(video.Files), "No suitable video file found"),
        };
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
                Wait = 15, // TODO: make configurable
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
            _logger.LogError("Long poll history request failed. Trying again. (error code: {ErrorCode}, message: {Message})",
                apiEx.ErrorCode, apiEx.Message);
            throw;
        }

        _ts = longPollResponse.Ts;

        return longPollResponse;
    }
}