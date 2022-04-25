using VkNet.Model.GroupUpdate;
namespace Vk2Tg.Services;

public interface IVkUpdateSourceService
{
    Task StartReceiveLoopAsync(CancellationToken cancellationToken);
    event Func<GroupUpdate, Task> GroupUpdate;
}
