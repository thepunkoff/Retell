namespace Vk2Tg.Elements;

public abstract class TgElement
{
    public TgElement AddElement(TgElement other)
    {
        return other switch
        {
            TgText text => AddText(text),
            TgPhoto photo => AddPhoto(photo),
            TgVideo video => AddVideo(video),
            TgPoll poll => AddPoll(poll),
            TgGif gif => AddGif(gif),
            TgLink link => AddLink(link),
            _ => throw new NotSupportedException($"Element type '{other.GetType().FullName}' is not supported for merging.")
        };
    }
    
    public abstract TgElement AddText(TgText text);
    
    public abstract TgElement AddPhoto(TgPhoto photo);
    
    public abstract TgElement AddVideo(TgVideo video);
    
    public abstract TgElement AddPoll(TgPoll poll);
    
    public abstract TgElement AddLink(TgLink link);
    
    public abstract TgElement AddGif(TgGif gif);

    public abstract Task Render(TgRenderContext context, CancellationToken token);

    public abstract DebugRenderToken[] DebugRender();
}