namespace Retell.Core.Models;

/// <summary>
/// Domain model of a generic post.
/// </summary>
public class Post
{
    /// <summary>
    /// Text content.
    /// </summary>
    public string Text { get; }

    /// <summary>
    /// Media attachments.
    /// </summary>
    public Medium[]? Media { get; }

    /// <summary>
    /// Poll.
    /// </summary>
    public Poll? Poll { get; }

    /// <summary>
    /// Link.
    /// </summary>
    public Uri[]? Links { get; }

    /// <summary>
    /// Domain model of a generic post.
    /// </summary>
    public Post(string text, Medium[]? media, Poll? poll, Uri[]? links)
    {
        Text = text;
        Media = media;
        Poll = poll;
        Links = links;
    }
}