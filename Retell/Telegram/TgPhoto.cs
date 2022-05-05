using Retell.Core.Models;
using Retell.Elements;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InputFiles;

namespace Retell.Telegram;

public class TgPhoto : TgElement, ITgMediaGroupElement
{
    private readonly bool _textUp;

    public Uri Url { get; }
    public string? Caption { get; set; }

    public MediumType Type => MediumType.Photo;

    public TgPhoto(Uri url, string? caption = null, bool textUp = false)
    {
        Url = url;
        Caption = caption;
        _textUp = textUp;
    }

    public override TgElement AddText(TgText text)
    {
        return new TgPhoto(Url, Caption is null ? text.Text : Caption + "\n\n" + text.Text);
    }

    public override TgElement AddPhoto(TgPhoto photo)
    {
        return new TgMediaGroup(new[] { (ITgMediaGroupElement)this, photo }, _textUp);
    }

    public override TgElement AddVideo(TgVideo video)
    {
        return new TgMediaGroup(new[] { (ITgMediaGroupElement)this, video }, _textUp);
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
        if (gif.Caption is not null)
            throw new NotSupportedException("Adding non null caption when merging gif is not supported.");

        if (Caption is null)
            return new TgCompoundElement(this, gif);

        if (MediaGroupMode is GifMediaGroupMode.TextUp)
            return new TgCompoundElement(new TgPhoto(Url, Caption, textUp: true), gif);

        return Caption.Length <= 1024
            ? new TgCompoundElement(new TgPhoto(Url), new TgGif(gif.Url, Caption))
            : new TgCompoundElement(this, gif);
    }

    public override async Task Render(TgRenderContext context, CancellationToken token)
    {
        if (!_textUp && (Caption is null || Caption.Length <= 1024))
        {
            var inputOnlineFile = new InputOnlineFile(Url);
            await Helpers.TelegramRetryForeverPolicy.ExecuteAsync(
                async t => await context.BotClient.SendPhotoAsync(context.ChatId, inputOnlineFile, Caption, cancellationToken: t),
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

    public override DebugRenderToken[] DebugRender()
    {
        if (Caption is null)
            return new[] { new DebugRenderToken(DebugRenderTokenType.Photo) };

        return _textUp
            ? new[] { new DebugRenderToken(DebugRenderTokenType.TextWithHtmlPhoto) }
            : Caption.Length <= 1024
                ? new[] { new DebugRenderToken(DebugRenderTokenType.PhotoWithCaption) }
                : new[] { new DebugRenderToken(DebugRenderTokenType.TextWithHtmlPhoto) };
    }

    public override string ToString()
    {
        if (Caption is null)
            return "[Photo]";

        return _textUp
            ? "[Text with HTML photo]"
            : Caption.Length <= 1024
                ? "[Photo with caption]"
                : "[Text with HTML photo]";
    }
}