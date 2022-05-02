using NLog;
using Vk2Tg.Elements;

namespace Vk2Tg.Telegram;

public class TgNullElement : TgElement
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public override TgElement AddText(TgText text) => text;

    public override TgElement AddPhoto(TgPhoto photo) => photo;

    public override TgElement AddVideo(TgVideo video) => video;

    public override TgElement AddPoll(TgPoll poll) => poll;
    public override TgElement AddLink(TgLink link) => link;

    public override TgElement AddGif(TgGif gif) => gif;

    public override Task Render(TgRenderContext context, CancellationToken token)
    {
        Logger.Warn("Null element was rendered!");
        return Task.CompletedTask;
    }

    public override DebugRenderToken[] DebugRender()
    {
        throw new NotImplementedException();
    }
}