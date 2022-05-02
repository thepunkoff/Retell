using Vk2Tg.Core.Models;

namespace Vk2Tg.Telegram;

public interface ITgMediaGroupElement
{
    Uri Url { get; }

    MediumType Type { get; }

    string? Caption { get; set; }
}