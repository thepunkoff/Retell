using Retell.Core.Models;

namespace Retell.Telegram;

public interface ITgMediaGroupElement
{
    Uri Url { get; }

    MediumType Type { get; }

    string? Caption { get; set; }
}