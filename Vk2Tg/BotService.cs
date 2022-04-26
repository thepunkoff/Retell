using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Vk2Tg.Abstractions.Services;
using Vk2Tg.Configuration;
using Vk2Tg.Elements;
using Vk2Tg.Filtering;
using Vk2Tg.Services;
using VkNet.Abstractions;
using VkNet.Enums.Filters;
using VkNet.Model;
using VkNet.Model.Attachments;
using VkNet.Model.GroupUpdate;
using VkNet.Model.RequestParams;

namespace Vk2Tg;

public partial class BotService : BackgroundService
{
    private readonly IVkApi _vkApi;
    private readonly ILogger<BotService> _logger;
    private readonly IConfiguration _configuration;
    private readonly IVkUpdateSourceService _vkUpdateSourceService;
    private readonly TelegramBotClient _tgBotClient;
    private readonly HttpClient _httpClient;
    private readonly IExceptionReportService _reportService;
    private readonly VkPostFilterService _postFilterService;
    public BotService(IVkApi vkApi,
        ILogger<BotService> logger,
        IConfiguration configuration,
        IVkUpdateSourceService vkUpdateSourceService,
        ITelegramBotClient tgBotClient,
        HttpClient httpClient,
        IExceptionReportService reportService,
        VkPostFilterService postFilterService)
    {
        _vkApi = vkApi;
        _logger = logger;
        _configuration = configuration;
        _vkUpdateSourceService = vkUpdateSourceService;
        _tgBotClient = (TelegramBotClient) tgBotClient;
        _httpClient = httpClient;
        _reportService = reportService;
        _postFilterService = postFilterService;
    }
    
#region Logging
    [LoggerMessage(1, LogLevel.Information, "New community wall post detected: '{ShortPostString}'")]
    partial void LogNewCommunityPost(string shortPostString);
    [LoggerMessage(2, LogLevel.Information, "Filtering result: {FilteringResult}")]
    partial void LogFilteringResult(FilteringResult filteringResult);
    [LoggerMessage(3, LogLevel.Debug, "Added {ElementName}. Result: {Element}")]
    partial void LogAddedElement(string elementName, TgElement element);
#endregion

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Initializing Vk2Tg...");

        _logger.LogTrace("Authorizing vk...");

        var vkSecrets = _configuration.Get<VkSecrets>();

        await _vkApi.AuthorizeAsync(new ApiAuthParams
        {
            AccessToken = vkSecrets.VkToken,
            Settings = Settings.All | Settings.Offline,
            TwoFactorAuthorization = Console.ReadLine,
        });
        _logger.LogTrace("Authorized vk");

        _logger.LogTrace("Starting update polling...");
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await foreach (var groupUpdate in _vkUpdateSourceService.GetGroupUpdatesAsync(stoppingToken))
                {
                    await ProcessGroupUpdate(groupUpdate);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Exception in update source, restarting loop");
                await _reportService.SendExceptionAsync(e);
            }
        }
    }
    
    private async Task ProcessGroupUpdate(GroupUpdate update)
    {
        if (update.WallPost is null)
            return;

        if (update.WallPost.FromId is null)
        {
            _logger.LogWarning("No FromId in the long poll response");
            return;
        }

        if (update.WallPost.FromId != -(long)_configuration.GetSection("vkGroupId").Get<ulong>())
            return;

        var shortPostString = update.WallPost.Text is not null
            ? update.WallPost.Text.Length > 100
                ? update.WallPost.Text[..100]
                : update.WallPost.Text
            : "no text in post";

        LogNewCommunityPost(shortPostString);

        var filteringResult = _postFilterService.Filter(update.WallPost);
        LogFilteringResult(filteringResult);
        if (filteringResult is not FilteringResult.ShouldShow)
            return;

        var tgElement = CreateTgElement(update.WallPost);

        _logger.LogDebug("Rendering TgElement...");
        await tgElement.Render(new TgRenderContext(_tgBotClient,
            _configuration.GetSection("telegramChatId").Get<long>(), _httpClient), default);
        _logger.LogDebug("TgElement rendered");
    }

    private TgElement CreateTgElement(Post post)
    {
        _logger.LogDebug("Creating TgElement...");
        TgElement ret = new TgNullElement();

        if (!string.IsNullOrWhiteSpace(post.Text))
        {
            var clearHashtags = _configuration.GetSection("clearHashtags").Get<bool>();
            var text = clearHashtags
                ? post.Text.RemoveHashtags()
                : post.Text;

            ret = ret.AddText(new TgText(text));
            LogAddedElement(clearHashtags ? "text. Removed hashtags" : "text", ret);
        }

        TgElement.MediaGroupMode = _configuration.GetSection("gifMediaGroupMode").Get<GifMediaGroupMode>();
        foreach (var attachment in post.Attachments)
        {
            switch (attachment.Instance)
            {
                case Photo photo:
                    ret = ret.AddPhoto(new TgPhoto(photo.Sizes.Last().Url));
                    LogAddedElement("photo", ret);
                    break;
                case Video video:
                    var videoCollection = _vkApi.Video.Get(new VideoGetParams {Videos = new[] {video}});
                    ret = ret.AddVideo(new TgVideo(ChoseBestQualityUrl(videoCollection[0])));
                    LogAddedElement("video", ret);
                    break;
                case Poll poll:
                    ret = ret.AddPoll(new TgPoll(poll.Question, poll.Answers.Select(x => x.Text).ToArray(), poll.Multiple ?? false));
                    LogAddedElement("poll", ret);
                    break;
                case Link link:
                    ret = ret.AddLink(new TgLink(link.Uri.ToString()));
                    LogAddedElement("link", ret);
                    break;
                case Document {Ext: "gif"} doc:
                    ret = ret.AddGif(new TgGif(new Uri(doc.Uri)));
                    LogAddedElement("gif", ret);
                    break;
            }
        }

        _logger.LogDebug("TgElement created");
        return ret;
    }
    
    private static Uri ChoseBestQualityUrl(Video video)
    {
        return video.Files switch
        {
            {External: { }} => video.Files.External,
            {Mp4_1080: { }} => video.Files.Mp4_1080,
            {Mp4_720: { }} => video.Files.Mp4_720,
            {Mp4_480: { }} => video.Files.Mp4_480,
            {Mp4_360: { }} => video.Files.Mp4_360,
            {Mp4_240: { }} => video.Files.Mp4_240,
            _ => throw new ArgumentOutOfRangeException(nameof(video.Files), "No suitable video file found")
        };
    }
}
