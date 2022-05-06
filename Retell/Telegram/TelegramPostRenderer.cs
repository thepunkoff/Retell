using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Retell.Core;
using Retell.Core.Models;
using Telegram.Bot;

namespace Retell.Telegram;

public class TelegramPostRenderer : IPostRenderer
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<TelegramPostRenderer> _logger;
    private readonly ITelegramBotClient _telegramBotClient;
    private readonly HttpClient _httpClient;
    private readonly long _chatId;

    public TelegramPostRenderer(
        ITelegramBotClient telegramBotClient,
        ILogger<TelegramPostRenderer> logger,
        HttpClient httpClient,
        IConfiguration configuration)
    {
        _configuration = configuration;
        _logger = logger;
        _telegramBotClient = telegramBotClient;
        _httpClient = httpClient;
        _chatId = configuration.GetSection("telegramChatId").Get<long>();
    }

    /// <inheritdoc />
    public async Task RenderAsync(Core.Models.Post post, CancellationToken token)
    {
        var element = CreateTgElement(post);
        await element.Render(new TgRenderContext(_telegramBotClient, _chatId, _httpClient), token);
    }

    private TgElement CreateTgElement(Core.Models.Post post)
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
            _logger.LogDebug("Added text.{Message} Result: {Ret}", clearHashtags ? " Removed hashtags." : string.Empty, ret);
        }

        // TODO: don't set every time
        TgElement.MediaGroupMode = _configuration.GetSection("gifMediaGroupMode").Get<GifMediaGroupMode>();

        if (post.Media is not null)
        {
            foreach (var medium in post.Media)
            {
                switch (medium.Type)
                {
                    case MediumType.Photo:
                        ret = ret.AddPhoto(new TgPhoto(medium.Uri));
                        _logger.LogDebug($"Added photo. Result: {ret}");
                        break;
                    case MediumType.Video:
                        ret = ret.AddVideo(new TgVideo(medium.Uri));
                        _logger.LogDebug($"Added video. Result: {ret}");
                        break;
                    case MediumType.Gif:
                        ret = ret.AddGif(new TgGif(medium.Uri));
                        _logger.LogDebug($"Added gif. Result: {ret}");
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        if (post.Poll is not null)
        {
            ret = ret.AddPoll(new TgPoll(post.Poll.Question, post.Poll.Options.ToArray(), post.Poll.AllowMultipleOptions));
            _logger.LogDebug($"Added poll. Result: {ret}");
        }

        if (post.Links is not null)
        {
            foreach (var link in post.Links)
            {
                ret = ret.AddLink(new TgLink(link.ToString()));
                _logger.LogDebug($"Added link. Result: {ret}");
            }
        }

        _logger.LogDebug("TgElement created.");
        return ret;
    }
}