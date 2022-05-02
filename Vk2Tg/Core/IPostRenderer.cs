using Vk2Tg.Core.Models;

namespace Vk2Tg.Core;

/// <summary>
/// Makes posts to the target platform.
/// </summary>
public interface IPostRenderer
{
    /// <summary>
    /// Render a post (make a post to the target platform).
    /// </summary>
    /// <param name="post">Post to render.</param>
    /// <param name="token">Cancellation token.</param>
    Task Render(Post post, CancellationToken token);
}