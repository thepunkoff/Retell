using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InputFiles;

namespace Vk2Tg.Elements;

public class TgGif : TgElement
{
    protected readonly bool _captionsHasHtml;

    public Uri Url { get; }

    public string? Caption { get; set; }

    public override Type[] Mergeables { get; }

    public override TgElement AddText(TgText text)
    {
        return new TgGif(Url, Caption is null ? text.Text : Caption + "\n\n" + text.Text);
    }

    public override TgElement AddPhoto(TgPhoto photo)
    {
        return Caption is not null
            ? new TgCompoundElement(new TgGif(Url), new TgPhoto(photo.Url, Caption)) 
            : new TgCompoundElement(this, photo);
    }

    public override TgElement AddVideo(TgVideo video)
    {
        return Caption is not null
            ? new TgCompoundElement(new TgGif(Url), new TgPhoto(video.Url, Caption)) 
            : new TgCompoundElement(this, video);
    }

    public override TgElement AddPoll(TgPoll poll)
    {
        return new TgCompoundElement(this, poll);
    }

    public override TgElement AddLink(TgLink link)
    {
        return new TgCompoundElement(this, link);
    }

    public override TgElement AddGif(TgGif gif)
    {
        return Caption is not null
            ? new TgCompoundElement(new TgGif(Url), new TgGif(gif.Url, Caption))
            : new TgCompoundElement(this, gif);
    }

    public TgGif(Uri url, string? caption = null)
    {
        Url = url;
        if (caption is null)
        {
            Caption = caption;
        }
        else
        {
            _captionsHasHtml = Helpers.TryTransformLinksVkToTelegram(caption, out var result);
            Caption = result;
        }
    }

    public override async Task Render(TgRenderContext context, CancellationToken token)
    {
        if (Caption is null || Caption?.Length <= 1024)
        {
            var inputOnlineFile = new InputOnlineFile(Url);
            await Helpers.TelegramRetryForeverPolicy.ExecuteAsync(
                async t => await context.BotClient.SendDocumentAsync(context.ChatId, inputOnlineFile, caption: Caption, cancellationToken: t, parseMode: _captionsHasHtml ? ParseMode.Html : null),
                token);
        }
        else
        {
            var gifHtml = $"<a href=\"{Url}\">⁠</a>";
            await Helpers.TelegramRetryForeverPolicy.ExecuteAsync(
                async t => await context.BotClient.SendTextMessageAsync(context.ChatId,  gifHtml + Caption, cancellationToken: t, parseMode: ParseMode.Html),
                token);
        }
    }
}