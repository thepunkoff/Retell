using Vk2Tg.Elements;

namespace Vk2Tg.Telegram;

/// <summary>
/// Compound element consists of elements that can't be merged together.
/// It's always ONE element that can be merged with something and AT LEAST ONE element that can't be merged with anything.
/// </summary>
public class TgCompoundElement : TgElement
{
    private readonly TgElement _first;
    private readonly TgElement _second;

    public TgCompoundElement(TgElement first, TgElement second)
    {
        _first = first;
        _second = second;
    }

    public override TgElement AddText(TgText text)
    {
        return new TgCompoundElement(_first.AddText(text), _second);
    }

    public override TgElement AddPhoto(TgPhoto photo)
    {
        return new TgCompoundElement(_first.AddPhoto(photo), _second);
    }

    public override TgElement AddVideo(TgVideo video)
    {
        return new TgCompoundElement(_first.AddVideo(video), _second);
    }

    public override TgElement AddPoll(TgPoll poll)
    {
        return new TgCompoundElement(_first, _second.AddPoll(poll));
    }

    public override TgElement AddLink(TgLink link)
    {
        return new TgCompoundElement(_first, _second.AddLink(link));
    }

    public override TgElement AddGif(TgGif gif)
    {
        return new TgCompoundElement(_first, _second.AddGif(gif));
    }


    public override async Task Render(TgRenderContext context, CancellationToken token)
    {
        await _first.Render(context, token);
        await _second.Render(context, token);
    }

    public override DebugRenderToken[] DebugRender()
    {
        return _first.DebugRender().Concat(_second.DebugRender()).ToArray();
    }

    public override string ToString()
    {
        return $"[{{{_first}, {_first}}}]";
    }
}