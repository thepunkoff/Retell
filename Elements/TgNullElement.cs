namespace Vk2Tg.Elements;

public class TgNullElement : TgElement
{
    public override Type[] Mergeables { get; } =
    {
        typeof(TgText),
        typeof(TgPhoto),
        typeof(TgVideo),
        typeof(TgMediaGroup),
        typeof(TgPoll),
        typeof(TgNullElement),
    };

    public override TgElement AddText(TgText text) => text;

    public override TgElement AddPhoto(TgPhoto photo) => photo;

    public override TgElement AddVideo(TgVideo video) => video;

    public override TgElement AddPoll(TgPoll poll) => poll;
    public override TgElement AddLink(TgLink link) => link;

    public override TgElement AddGif(TgGif gif) => gif;

    public override Task Render(TgRenderContext context, CancellationToken token)
    {
        Console.WriteLine("Null element was rendered!");
        return Task.CompletedTask;
    }
}