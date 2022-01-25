namespace Vk2Tg.Elements;

/// <summary>
/// Compound element consists of elements that can't be merged together.
/// It's always ONE element that can be merged with something and AT LEAST ONE element that can't be merged with anything.  
/// </summary>
public class TgCompoundElement : TgElement
{
    private TgElement _first;
    private TgElement _second;

    public TgCompoundElement(TgElement first, TgElement second)
    {
        _first = first;
        _second = second;
    }

    public override Type[] Mergeables => throw new InvalidOperationException();

    public override TgElement AddText(TgText text)
    {
        _first = _first.AddText(text);
        return this;
    }

    public override TgElement AddPhoto(TgPhoto photo)
    {
        _first = _first.AddPhoto(photo);
        return this;
    }

    public override TgElement AddVideo(TgVideo video)
    {
        _first = _first.AddVideo(video);
        return this;
    }

    public override TgElement AddPoll(TgPoll poll)
    {
        _second = _second.AddPoll(poll);
        return this;
    }

    public override TgElement AddLink(TgLink link)
    {
        _second = _second.AddLink(link);
        return this;
    }

    public override TgElement AddGif(TgGif gif)
    {
        _first = _first.AddGif(gif);
        return this;
    }


    public override async Task Render(TgRenderContext context, CancellationToken token)
    {
        await _first.Render(context, token);
        await _second.Render(context, token);
    }
}