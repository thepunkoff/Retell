namespace Vk2Tg.Elements;

public abstract class TgElement
{
    public abstract Type[] Mergeables { get; }

    public TgElement AddElement(TgElement other)
    {
        if (!Mergeables.Contains(other.GetType()))
            throw new NotSupportedException("Merge should be already possible.");

        return other switch
        {
            TgText text => AddText(text),
            TgPhoto photo => AddPhoto(photo),
            TgVideo video => AddVideo(video),
            TgPoll poll => AddPoll(poll),
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
}