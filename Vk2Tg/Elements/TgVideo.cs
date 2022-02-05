using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InputFiles;

namespace Vk2Tg.Elements;

public class TgVideo : TgElement, IMediaGroupElement
{
    protected readonly bool _captionsHasHtml;
    private readonly bool _textUpIfLongCaption;

    public Uri Url { get; }
    public string? Caption { get; set; }

    public virtual MediumType Type => MediumType.Video;
    
    public override Type[] Mergeables { get; } =
    {
        typeof(TgText),
        typeof(TgPhoto),
        typeof(TgVideo),
        typeof(TgNullElement),
        typeof(TgMediaGroup),
    };

    public TgVideo(Uri url, string? caption = null, bool captionsHasHtml = false, bool textUpIfLongCaption = false)
    {
        Url = url;
        Caption = caption;
        _captionsHasHtml = captionsHasHtml;
        _textUpIfLongCaption = textUpIfLongCaption;
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
        return new TgCompoundElement(Caption == poll.Question ? new TgVideo(Url) : this, poll);
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
            return new TgCompoundElement(new TgVideo(Url, Caption, textUpIfLongCaption: true), gif);

        return Caption.Length <= 1024
            ? new TgCompoundElement(new TgVideo(Url), new TgGif(gif.Url, Caption))
            : new TgCompoundElement(new TgVideo(Url, Caption, textUpIfLongCaption: true), gif);
    }

    public override async Task Render(TgRenderContext context, CancellationToken token)
    {
        if (Caption is null || Caption.Length <= 1024)
        {
            await using var stream = await context.HttpClient.GetStreamAsync(Url, token);
            var inputOnlineFile = new InputOnlineFile(stream);
            await Helpers.TelegramRetryForeverPolicy.ExecuteAsync(
                async t => await context.BotClient.SendVideoAsync(context.ChatId, inputOnlineFile, caption: Caption, cancellationToken: t, parseMode: _captionsHasHtml ? ParseMode.Html : null),
                token);
            return;
        }

        Message? firstPart = null;

        if (_textUpIfLongCaption)
        {
            await Helpers.TelegramRetryForeverPolicy.ExecuteAsync(
                async t => { firstPart = await context.BotClient.SendTextMessageAsync(context.ChatId, Caption, cancellationToken: t); },
                token);
                
            await using var stream = await context.HttpClient.GetStreamAsync(Url, token);
            var inputOnlineFile = new InputOnlineFile(stream);
            await Helpers.TelegramRetryForeverPolicy.ExecuteAsync(
                async t => { await context.BotClient.SendVideoAsync(context.ChatId, inputOnlineFile, cancellationToken: t, parseMode: _captionsHasHtml ? ParseMode.Html : null,  replyToMessageId: firstPart!.MessageId); },
                token);
        }
        else
        {
                
            await using var stream = await context.HttpClient.GetStreamAsync(Url, token);
            var inputOnlineFile = new InputOnlineFile(stream);
            await Helpers.TelegramRetryForeverPolicy.ExecuteAsync(
                async t => { firstPart = await context.BotClient.SendVideoAsync(context.ChatId, inputOnlineFile, cancellationToken: t, parseMode: _captionsHasHtml ? ParseMode.Html : null); },
                token);

            await Helpers.TelegramRetryForeverPolicy.ExecuteAsync(
                async t => { await context.BotClient.SendTextMessageAsync(context.ChatId, Caption, cancellationToken: t, replyToMessageId: firstPart!.MessageId); },
                token);
        }
    }
}