using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace Vk2Tg.Elements;

public class TgText : TgElement
{
    public readonly string Text;
    private readonly bool _hasHtml;

    public override Type[] Mergeables { get; } =
    {
        typeof(TgText),
        typeof(TgPhoto),
        typeof(TgVideo),
        typeof(TgNullElement),
        typeof(TgMediaGroup),
    };
    
    public TgText(string text)
    {
        _hasHtml = Helpers.TryTransformLinksVkToTelegram(text, out Text);
    }

    public override TgElement AddText(TgText text)
    {
        return new TgText(Text + "\n\n" + text.Text);
    }

    public override TgElement AddPhoto(TgPhoto photo)
    {
        return new TgPhoto(photo.Url, Text);
    }

    public override TgElement AddVideo(TgVideo video)
    {
        return new TgVideo(video.Url, Text);
    }

    public override TgElement AddPoll(TgPoll poll)
    {
        return new TgPoll(Text == poll.Question ? Text : Text + "\n\n" + poll.Question, poll.Options, poll.AllowMultipleOptions);
    }

    public override TgElement AddLink(TgLink link)
    {
        return new TgText(Text + "\n\n" + link.Link);
    }

    public override TgElement AddGif(TgGif gif)
    {
        return new TgGif(gif.Url, Text);
    }

    public override async Task Render(TgRenderContext context, CancellationToken token)
    {
        await Helpers.TelegramRetryForeverPolicy.ExecuteAsync(
            async t => await context.BotClient.SendTextMessageAsync(context.ChatId, Text, cancellationToken: t, parseMode: _hasHtml ? ParseMode.Html : null),
            token);
    }
}