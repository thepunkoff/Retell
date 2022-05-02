using Vk2Tg.Core.Models;

namespace Vk2Tg.Core;

public interface IDriver
{
    IAsyncEnumerable<Post> GetPosts(CancellationToken token);
    Task RenderPost(Post post);
}