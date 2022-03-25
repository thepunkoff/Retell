using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InputFiles;

namespace Vk2Tg.Elements;

// TODO: gif - расширение photo (или назвать как-то по другому то, что у них общее (всё, кроме способа получения ресурса))
public class TgGif : TgElement
{
    private readonly bool _textUp;

    public Uri Url { get; }

    public string? Caption { get; set; }
    
    public TgGif(Uri url, string? caption = null, bool textUp = false)
    {
        Url = url;
        Caption = caption;
        _textUp = textUp;
    }

    public override TgElement AddText(TgText text)
    {
        return new TgGif(Url, Caption is null ? text.Text : Caption + "\n\n" + text.Text);
    }

    public override TgElement AddPhoto(TgPhoto photo)
    {
        if (photo.Caption is not null)
            throw new NotSupportedException("Adding non null caption when merging gif is not supported.");
        
        if (Caption is null)
            return new TgCompoundElement(photo, this);

        if (Vk2TgConfig.Current.GifMediaGroupMode is GifMediaGroupMode.TextUp)
            return new TgCompoundElement(new TgPhoto(photo.Url, Caption, textUp: true), new TgGif(Url));

        return Caption.Length <= 1024
            ? new TgCompoundElement(photo, this)
            : new TgCompoundElement(new TgPhoto(photo.Url, Caption), new TgGif(Url));
    }

    public override TgElement AddVideo(TgVideo video)
    {
        if (video.Caption is not null)
            throw new NotSupportedException("Adding non null caption when merging gif is not supported.");
        
        if (Caption is null)
            return new TgCompoundElement(video, this);

        if (Vk2TgConfig.Current.GifMediaGroupMode is GifMediaGroupMode.TextUp)
            return new TgCompoundElement(new TgVideo(video.Url, Caption, textUp: true), new TgGif(Url));

        return Caption.Length <= 1024
            ? new TgCompoundElement(video, this)
            : new TgCompoundElement(new TgVideo(video.Url, Caption), new TgGif(Url));
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
        if (gif.Caption is not null)
            throw new NotSupportedException("Adding non null caption when merging gif is not supported.");
        
        if (Caption is null || Vk2TgConfig.Current.GifMediaGroupMode is GifMediaGroupMode.TextUp)
            return new TgCompoundElement(this, gif);

        return Caption.Length <= 1024
            ? new TgCompoundElement(new TgGif(Url), new TgPhoto(gif.Url, Caption))
            : new TgCompoundElement(this, gif);
    }

    public override async Task Render(TgRenderContext context, CancellationToken token)
    {
        if (!_textUp && (Caption is null || Caption.Length <= 1024))
        {
            var inputOnlineFile = new InputOnlineFile(Url);
            await Helpers.TelegramRetryForeverPolicy.ExecuteAsync(
                async t => await context.BotClient.SendDocumentAsync(context.ChatId, inputOnlineFile, caption: Caption, cancellationToken: t),
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

    public override DebugRenderToken[] DebugRender()
    {
        if (Caption is null)
            return new[] { new DebugRenderToken(DebugRenderTokenType.Gif) };

        return _textUp
            ? new[] { new DebugRenderToken(DebugRenderTokenType.TextWithHtmlGif) }
            : Caption.Length <= 1024
                ? new[] { new DebugRenderToken(DebugRenderTokenType.GifWithCaption) }
                : new[] { new DebugRenderToken(DebugRenderTokenType.TextWithHtmlGif) };
    }

    public override string ToString()
    {
        if (Caption is null)
            return "[Git]";

        return _textUp
            ? "[Text with HTML gif]"
            : Caption.Length <= 1024
                ? "[Gif with caption]"
                : "[Text with HTML gif]";
    }
}