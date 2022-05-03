using Retell.Core.Models;

namespace Retell.Core;

/// <summary>
/// Source from which the posts are consumed.
/// </summary>
public interface IPostSource
{
    /// <summary>
    /// Start consuming posts.
    /// </summary>
    /// <param name="token">Cancellation token.</param>
    IAsyncEnumerable<Post> GetPosts(CancellationToken token);
}