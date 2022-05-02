using System.Security.Cryptography;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Vk2Tg.Core.Models;
using Vk2Tg.Elements;

namespace Vk2Tg.Telegram;

public class TgMediaGroup : TgElement
{
    private readonly List<ITgMediaGroupElement> _media = new ();
    private readonly bool _textUp;

    public TgMediaGroup(IEnumerable<ITgMediaGroupElement> media, bool textUp = false)
    {
        _media.AddRange(media);
        _textUp = textUp;
    }

    public override TgElement AddText(TgText text)
    {
        var copy = new TgMediaGroup(_media);
        copy._media[0].Caption = _media[0].Caption is null ? text.Text : _media[0].Caption + "\n\n" + text.Text;
        return copy;
    }

    public override TgElement AddPhoto(TgPhoto photo)
    {
        var copy = new TgMediaGroup(_media);
        copy.AddMedium(photo);
        return copy;
    }

    public override TgElement AddVideo(TgVideo video)
    {
        var copy = new TgMediaGroup(_media);
        copy.AddMedium(video);
        return copy;
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
        return new TgCompoundElement(this, gif);
    }

    public void AddMedium(ITgMediaGroupElement tgMedium) => _media.Add(tgMedium);

    public override async Task Render(TgRenderContext context, CancellationToken token)
    {
        var mediaStreams = new List<Stream>();
        var inputMedia = new List<IAlbumInputMedia>();

        // TODO: merge all captions to the first, so that it was visible
        foreach (var medium in _media)
        {
            var stream = await context.HttpClient.GetStreamAsync(medium.Url, token);
            mediaStreams.Add(stream);

            var inputMedium = new InputMedia(stream, RandomNumberGenerator.GetInt32(int.MinValue, int.MaxValue).ToString());
            inputMedia.Add(medium.Type switch
            {
                MediumType.Photo => new InputMediaPhoto(inputMedium) { Caption = medium.Caption  },
                MediumType.Video => new InputMediaVideo(inputMedium) { Caption = medium.Caption },
                _ => throw new NotSupportedException($"Medium type '{medium.Type}' is not supported")
            });
        }

        do
        {
            if (inputMedia[0].Caption is null)
            {
                await SendOneMessage(context, inputMedia, token);
                break;
            }

            if (_textUp)
            {
                await SendTextReplyWithMediaGroup(context, inputMedia, token);
                break;
            }

            if (inputMedia[0].Caption!.Length <= 1024)
            {
                await SendOneMessage(context, inputMedia, token);
                break;
            }

            await SendMediaGroupReplyWithText(context, inputMedia, token);

        } while (false);

        foreach (var stream in mediaStreams)
        {
            stream.Close();
            await stream.DisposeAsync();
        }
    }

    private async Task SendOneMessage(TgRenderContext context, List<IAlbumInputMedia> inputMedia, CancellationToken token)
    {
        await Helpers.TelegramRetryForeverPolicy.ExecuteAsync(
            async t => await context.BotClient.SendMediaGroupAsync(context.ChatId, inputMedia, cancellationToken: t),
            token);
    }

    private async Task SendTextReplyWithMediaGroup(TgRenderContext context, List<IAlbumInputMedia> inputMedia, CancellationToken token)
    {
        var text = inputMedia[0].Caption!;
        inputMedia[0] = inputMedia[0].Type switch
        {
            InputMediaType.Photo => new InputMediaPhoto(inputMedia[0].Media),
            InputMediaType.Video => new InputMediaVideo(inputMedia[0].Media),
            _ => throw new NotSupportedException($"Medium type '{inputMedia[0].Media}' is not supported")
        };

        Message? firstPart = null;

        await Helpers.TelegramRetryForeverPolicy.ExecuteAsync(
            async t =>
            {
                firstPart = await context.BotClient.SendTextMessageAsync(context.ChatId, text, cancellationToken: t);
            },
            token);

        await Helpers.TelegramRetryForeverPolicy.ExecuteAsync(
            async t =>
            {
                await context.BotClient.SendMediaGroupAsync(context.ChatId, inputMedia, cancellationToken: t, replyToMessageId: firstPart!.MessageId);
            },
            token);
    }

    private async Task SendMediaGroupReplyWithText(TgRenderContext context, List<IAlbumInputMedia> inputMedia, CancellationToken token)
    {
        var text = inputMedia[0].Caption!;
        inputMedia[0] = inputMedia[0].Type switch
        {
            InputMediaType.Photo => new InputMediaPhoto(inputMedia[0].Media),
            InputMediaType.Video => new InputMediaVideo(inputMedia[0].Media),
            _ => throw new NotSupportedException($"Medium type '{inputMedia[0].Media}' is not supported")
        };

        Message? firstPart = null;
        await Helpers.TelegramRetryForeverPolicy.ExecuteAsync(
            async t =>
            {
                var msgs = await context.BotClient.SendMediaGroupAsync(context.ChatId, inputMedia, cancellationToken: t);
                firstPart = msgs[0];
            },
            token);

        await Helpers.TelegramRetryForeverPolicy.ExecuteAsync(
            async t =>
            {
                await context.BotClient.SendTextMessageAsync(context.ChatId, text, cancellationToken: t, replyToMessageId: firstPart!.MessageId);
            },
            token);
    }

    public override DebugRenderToken[] DebugRender()
    {
        if (_media[0].Caption is null)
            return new[] { new DebugRenderToken(DebugRenderTokenType.MediaGroup) };

        if (_textUp)
        {
            var text = new DebugRenderToken(_media[0].Caption!.Length <= 1024 ? DebugRenderTokenType.ShortText : DebugRenderTokenType.LongText);
            var tokens = new[] { text, new DebugRenderToken(DebugRenderTokenType.MediaGroup, text) };
            return tokens;
        }

        if (_media[0].Caption!.Length <= 1024)
            return new[] { new DebugRenderToken(DebugRenderTokenType.MediaGroupWithCaption) };

        var mediaGroup = new DebugRenderToken(DebugRenderTokenType.MediaGroup);
        return new[] { mediaGroup, new DebugRenderToken(DebugRenderTokenType.LongText, mediaGroup) };
    }

    public override string ToString()
    {
        var mediaString = string.Join(", ", _media.ToString());

        if (_media[0].Caption is null)
            return $"[Media group: {mediaString}]";

        if (_textUp)
            return $"{(_media[0].Caption!.Length <= 1024 ? "[Short text" : "[Long text")} replied by a media group {mediaString}]";

        return _media[0].Caption!.Length <= 1024
            ? $"[Media group with caption: {mediaString}]"
            : $"[Media group: {mediaString}. Replied by a long text]";
    }
}