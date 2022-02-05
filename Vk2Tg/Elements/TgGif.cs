using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InputFiles;

namespace Vk2Tg.Elements;

public class TgGif : TgElement
{
    private readonly bool _captionsHasHtml;
    private readonly bool _forceHtmlGif;

    public Uri Url { get; }

    public string? Caption { get; set; }

    public override Type[] Mergeables { get; }
    
    public TgGif(Uri url, string? caption = null, bool captionsHasHtml = false, bool forceHtmlGif = false)
    {
        Url = url;
        Caption = caption;
        _captionsHasHtml = captionsHasHtml;
        _forceHtmlGif = forceHtmlGif;
    }

    public override TgElement AddText(TgText text)
    {
        return new TgGif(Url, Caption is null ? text.Text : Caption + "\n\n" + text.Text);
    }

    public override TgElement AddPhoto(TgPhoto photo)
    {
        if (Caption is null)
            return new TgCompoundElement(this, photo);

        if (Vk2TgConfig.Current.GifMediaGroupMode is GifMediaGroupMode.TextUp)
            return new TgCompoundElement(new TgGif(Url, Caption, forceHtmlGif: true), photo);

        return Caption.Length <= 1024
            ? new TgCompoundElement(new TgGif(Url), new TgPhoto(Url, Caption))
            : new TgCompoundElement(this, photo);
    }

    public override TgElement AddVideo(TgVideo video)
    {
        if (Caption is null)
            return new TgCompoundElement(this, video);

        if (Vk2TgConfig.Current.GifMediaGroupMode is GifMediaGroupMode.TextUp)
            return new TgCompoundElement(new TgGif(Url, Caption, forceHtmlGif: true), video);

        return Caption.Length <= 1024
            ? new TgCompoundElement(new TgGif(Url), new TgVideo(Url, Caption))
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
        if (Caption is null || Vk2TgConfig.Current.GifMediaGroupMode is GifMediaGroupMode.TextUp)
            return new TgCompoundElement(this, gif);

        return Caption.Length <= 1024
            ? new TgCompoundElement(new TgGif(Url), new TgPhoto(gif.Url, Caption))
            : new TgCompoundElement(this, gif);
    }

    public override async Task Render(TgRenderContext context, CancellationToken token)
    {
        if (!_forceHtmlGif && (Caption is null || Caption.Length <= 1024))
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