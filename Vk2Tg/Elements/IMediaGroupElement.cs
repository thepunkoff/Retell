namespace Vk2Tg.Elements;

public interface IMediaGroupElement
{
    Uri Url { get; }
    
    MediumType Type { get; }
    
    string? Caption { get; set; }
}