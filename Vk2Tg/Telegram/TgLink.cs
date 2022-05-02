using Telegram.Bot;
using Vk2Tg.Elements;

namespace Vk2Tg.Telegram;

public class TgLink : TgElement
{
    public readonly string Link;

    public TgLink(string link)
    {
        Link = link;
    }

    public override TgElement AddText(TgText text)
    {
        return new TgText(Link + "\n\n" + text.Text);
    }

    public override TgElement AddPhoto(TgPhoto photo)
    {
        return new TgCompoundElement(photo, this);
    }

    public override TgElement AddVideo(TgVideo video)
    {
        return new TgCompoundElement(video, this);
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
        return new TgCompoundElement(gif, this);
    }

    public override async Task Render(TgRenderContext context, CancellationToken token)
    {
        await Helpers.TelegramRetryForeverPolicy.ExecuteAsync(
            async t => await context.BotClient.SendTextMessageAsync(context.ChatId, Link, cancellationToken: t),
            token);
    }

    public override DebugRenderToken[] DebugRender()
    {
        return new[] { new DebugRenderToken(DebugRenderTokenType.Link) };
    }

    public override string ToString()
    {
        return "[Link]";
    }
}