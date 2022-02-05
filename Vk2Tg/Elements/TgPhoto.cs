using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InputFiles;

namespace Vk2Tg.Elements;

public class TgPhoto : TgElement, IMediaGroupElement
{
    private readonly bool _captionsHasHtml;
    private readonly bool _forceHtmlPhoto;

    public Uri Url { get; }
    public string? Caption { get; set; }

    public MediumType Type => MediumType.Photo;

    public override Type[] Mergeables { get; } =
    {
        typeof(TgText),
        typeof(TgPhoto),
        typeof(TgVideo),
        typeof(TgNullElement),
        typeof(TgMediaGroup),
    };
    
    public TgPhoto(Uri url, string? caption = null, bool captionsHasHtml = false, bool forceHtmlPhoto = false)
    {
        Url = url;
        Caption = caption;
        _captionsHasHtml = captionsHasHtml;
        _forceHtmlPhoto = forceHtmlPhoto;
    }

    public override TgElement AddText(TgText text)
    {
        return new TgPhoto(Url, Caption is null ? text.Text : Caption + "\n\n" + text.Text);
    }

    public override TgElement AddPhoto(TgPhoto photo)
    {
        return new TgMediaGroup(new[] { (IMediaGroupElement)this, photo });
    }

    public override TgElement AddVideo(TgVideo video)
    {
        return new TgMediaGroup(new[] { (IMediaGroupElement)this, video });
    }

    public override TgElement AddPoll(TgPoll poll)
    {
        return new TgCompoundElement(Caption == poll.Question ? new TgPhoto(Url) : this , poll);
    }

    public override TgElement AddLink(TgLink link)
    {
        return new TgCompoundElement(this, link);
    }

    public override TgElement AddGif(TgGif gif)
    {
        if (Caption is null)
            return new TgCompoundElement(this, gif);

        if (Vk2TgConfig.Current.GifMediaGroupMode is GifMediaGroupMode.TextUp)
            return new TgCompoundElement(new TgPhoto(Url, Caption, forceHtmlPhoto: true), gif);

        return Caption.Length <= 1024
            ? new TgCompoundElement(new TgPhoto(Url), new TgGif(gif.Url, Caption))
            : new TgCompoundElement(this, gif);
    }

    public override async Task Render(TgRenderContext context, CancellationToken token)
    {
        if (!_forceHtmlPhoto && (Caption is null || Caption.Length <= 1024))
        {
            var inputOnlineFile = new InputOnlineFile(Url);
            await Helpers.TelegramRetryForeverPolicy.ExecuteAsync(
                async t => await context.BotClient.SendPhotoAsync(context.ChatId, inputOnlineFile, Caption, cancellationToken: t, parseMode: _captionsHasHtml ? ParseMode.Html : null),
                token);
        }
        else
        {
            var picHtml = $"<a href=\"{Url}\">⁠</a>";
            await Helpers.TelegramRetryForeverPolicy.ExecuteAsync(
                async t => await context.BotClient.SendTextMessageAsync(context.ChatId,  picHtml + Caption, cancellationToken: t, parseMode: ParseMode.Html),
                token);
        }
    }
}