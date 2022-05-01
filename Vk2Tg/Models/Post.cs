namespace Vk2Tg.Models;

/// <summary>
/// Domain model of a generic post.
/// </summary>
public class Post
{
    /// <summary>
    /// Text content.
    /// </summary>
    public string Text { get; set; }

    /// <summary>
    /// Media attachments.
    /// </summary>
    public Medium[] Media { get; set; }

    /// <summary>
    /// Domain model of a generic post.
    /// </summary>
    public Post(string text, Medium[] media)
    {
        Text = text;
        Media = media;
    }
}