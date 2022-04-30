using VkNet.Model.GroupUpdate;
namespace Vk2Tg.Abstractions.Services;

public interface IVkUpdateSourceService
{
    IAsyncEnumerable<GroupUpdate> GetGroupUpdatesAsync(CancellationToken cancellationToken);
}
