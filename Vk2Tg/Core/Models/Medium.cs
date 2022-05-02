using Vk2Tg.Elements;

namespace Vk2Tg.Core.Models;

/// <summary>
/// Domain model of a generic medium.
/// </summary>
public class Medium
{
    /// <summary>
    /// Medium type.
    /// </summary>
    public MediumType Type { get; set; }

    /// <summary>
    /// Medium uri.
    /// </summary>
    public Uri Uri { get; set; }

    /// <summary>
    /// Domain model for a generic medium.
    /// </summary>
    public Medium(MediumType type, Uri uri)
    {
        Type = type;
        Uri = uri;
    }
}