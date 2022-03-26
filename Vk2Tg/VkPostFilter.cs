using NLog;
using VkNet.Model.GroupUpdate;

namespace Vk2Tg;

public static class VkPostFilter
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public static bool ShouldShow(WallPost vkWallPost)
    {
        if (!DynamicSettings.IsBotEnabled)
        {
            Logger.Info("Post won't be shown: bot is disabled.");
            return false;
        }

        return true;
    }
}