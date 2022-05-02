using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.InputFiles;
using Vk2Tg.Core.Models;
using Vk2Tg.Elements;

namespace Vk2Tg.Telegram;

public class TgVideo : TgElement, ITgMediaGroupElement
{
    private readonly bool _textUp;

    public Uri Url { get; }
    public string? Caption { get; set; }

    public virtual MediumType Type => MediumType.Video;

    public TgVideo(Uri url, string? caption = null, bool textUp = false)
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
        return new TgCompoundElement(Caption == poll.Question ? new TgVideo(Url) : this, poll);
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
            return new TgCompoundElement(new TgVideo(Url, Caption, textUp: true), gif);

        return Caption.Length <= 1024
            ? new TgCompoundElement(new TgVideo(Url), new TgGif(gif.Url, Caption))
            : new TgCompoundElement(new TgVideo(Url, Caption), gif);
    }

    public override async Task Render(TgRenderContext context, CancellationToken token)
    {
        if (Caption is null)
        {
            await SendOneMessage(context, token);
            return;
        }

        if (_textUp)
        {
            await SendTextReplyWithVideo(context, token);
            return;
        }

        if (Caption.Length <= 1024)
        {
            await SendOneMessage(context, token);
            return;
        }

        await SendVideoReplyWithText(context, token);
    }

    private async Task SendOneMessage(TgRenderContext context, CancellationToken token)
    {
        await using var stream = await context.HttpClient.GetStreamAsync(Url, token);
        var inputOnlineFile = new InputOnlineFile(stream);
        await Helpers.TelegramRetryForeverPolicy.ExecuteAsync(
            async t => await context.BotClient.SendVideoAsync(context.ChatId, inputOnlineFile, caption: Caption, cancellationToken: t),
            token);
    }

    private async Task SendTextReplyWithVideo(TgRenderContext context, CancellationToken token)
    {
        Message? firstPart = null;
        await Helpers.TelegramRetryForeverPolicy.ExecuteAsync(
            async t => { firstPart = await context.BotClient.SendTextMessageAsync(context.ChatId, Caption!, cancellationToken: t); },
            token);

        await using var stream = await context.HttpClient.GetStreamAsync(Url, token);
        var inputOnlineFile = new InputOnlineFile(stream);
        await Helpers.TelegramRetryForeverPolicy.ExecuteAsync(
            async t => { await context.BotClient.SendVideoAsync(context.ChatId, inputOnlineFile, cancellationToken: t,  replyToMessageId: firstPart!.MessageId); },
            token);
    }

    private async Task SendVideoReplyWithText(TgRenderContext context, CancellationToken token)
    {
        Message? firstPart = null;
        await using var stream = await context.HttpClient.GetStreamAsync(Url, token);
        var inputOnlineFile = new InputOnlineFile(stream);
        await Helpers.TelegramRetryForeverPolicy.ExecuteAsync(
            async t => { firstPart = await context.BotClient.SendVideoAsync(context.ChatId, inputOnlineFile, cancellationToken: t); },
            token);

        await Helpers.TelegramRetryForeverPolicy.ExecuteAsync(
            async t => { await context.BotClient.SendTextMessageAsync(context.ChatId, Caption!, cancellationToken: t, replyToMessageId: firstPart!.MessageId); },
            token);
    }

    public override DebugRenderToken[] DebugRender()
    {
        if (Caption is null)
            return new[] { new DebugRenderToken(DebugRenderTokenType.Video) };

        if (_textUp)
        {
            var text = new DebugRenderToken(Caption.Length <= 1024 ? DebugRenderTokenType.ShortText : DebugRenderTokenType.LongText);
            var tokens = new[] { text, new DebugRenderToken(DebugRenderTokenType.Video, text) };
            return tokens;
        }

        if (Caption.Length <= 1024)
            return new[] { new DebugRenderToken(DebugRenderTokenType.VideoWithCaption) };

        var video = new DebugRenderToken(DebugRenderTokenType.Video);
        return new[] { video, new DebugRenderToken(Caption.Length <= 1024 ? DebugRenderTokenType.ShortText : DebugRenderTokenType.LongText, video) };
    }

    public override string ToString()
    {
        if (Caption is null)
            return "[Video]";

        if (_textUp)
            return $"{(Caption.Length <= 1024 ? "[Short text" : "[Long text")} replied by a video]";

        return Caption.Length <= 1024
            ? "[Video with caption]"
            : $"[Video replied by a {(Caption.Length <= 1024 ? "short text]" : "long text]")}";
    }
}