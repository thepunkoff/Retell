using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InputFiles;

namespace Vk2Tg.Elements;

public class TgVideo : TgElement, IMediaGroupElement
{
    protected readonly bool _captionsHasHtml;
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

    public TgVideo(Uri url, string? caption = null)
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
        return Caption is not null
            ? new TgCompoundElement(new TgPhoto(Url), new TgGif(gif.Url, Caption)) 
            : new TgCompoundElement(this, gif);
    }

    public override async Task Render(TgRenderContext context, CancellationToken token)
    {
        if (Caption is null || Caption.Length <= 1024)
        {
            // TODO: length!!!! (see TgPhoto.Render)
            await using var stream = await context.HttpClient.GetStreamAsync(Url, token);
            var inputOnlineFile = new InputOnlineFile(stream);
            await Helpers.TelegramRetryForeverPolicy.ExecuteAsync(
                async t => await context.BotClient.SendVideoAsync(context.ChatId, inputOnlineFile, caption: Caption, cancellationToken: t, parseMode: _captionsHasHtml ? ParseMode.Html : null),
                token);
        }
        else
        {
            Message? firstPart = null;
            
            await using var stream = await context.HttpClient.GetStreamAsync(Url, token);
            var inputOnlineFile = new InputOnlineFile(stream);
            await Helpers.TelegramRetryForeverPolicy.ExecuteAsync(
                async t =>
                {
                    firstPart = await context.BotClient.SendVideoAsync(context.ChatId, inputOnlineFile, cancellationToken: t, parseMode: _captionsHasHtml ? ParseMode.Html : null);
                },
                token);

            await Helpers.TelegramRetryForeverPolicy.ExecuteAsync(
                async t =>
                {
                    await context.BotClient.SendTextMessageAsync(context.ChatId, Caption, cancellationToken: t, replyToMessageId: firstPart!.MessageId);
                },
                token);
        }
    }
}