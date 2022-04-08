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

        if (DynamicSettings.SignalWords is null)
            return true;

        var signalWordFound = DynamicSettings.SignalWords.FirstOrDefault(x => vkWallPost.Text.Contains(x)) ?? (vkWallPost.Text.StartsWith(" ") ? "Unprinted symbol at the beginning of the text" : null);
        if (signalWordFound is null)
        {
            Logger.Info("Post won't be shown: signal words not found.");
            return false;
        }

        Logger.Info($"Signal word found: '{signalWordFound}'. Showing post.");

        return true;
    }
}