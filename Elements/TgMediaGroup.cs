using System.Security.Cryptography;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Vk2Tg.Elements;

public class TgMediaGroup : TgElement
{
    private readonly List<IMediaGroupElement> _media = new ();

    public override Type[] Mergeables { get; } =
    {
        typeof(TgText),
        typeof(TgPhoto),
        typeof(TgVideo),
        typeof(TgNullElement),
        typeof(TgMediaGroup),
    };
    
    public TgMediaGroup(IEnumerable<IMediaGroupElement> media)
    {
        _media.AddRange(media);
    }

    public override TgElement AddText(TgText text)
    {
        _media[0].Caption = _media[0].Caption is null ? text.Text : _media[0].Caption + "\n\n" + text.Text;
        return this;
    }

    public override TgElement AddPhoto(TgPhoto photo)
    {
        AddMedium(photo);
        return this;
    }

    public override TgElement AddVideo(TgVideo video)
    {
        AddMedium(video);
        return this;
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

    public void AddMedium(IMediaGroupElement medium) => _media.Add(medium);

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

        if (inputMedia[0].Caption is null || inputMedia[0].Caption?.Length <= 1024)
        {
            await Helpers.TelegramRetryForeverPolicy.ExecuteAsync(
                async t => await context.BotClient.SendMediaGroupAsync(context.ChatId, inputMedia, cancellationToken: t),
                token);
        }
        else
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

        foreach (var stream in mediaStreams)
        {
            stream.Close();
            await stream.DisposeAsync();
        }
    }
}