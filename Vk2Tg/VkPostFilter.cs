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
            Logger.Info($"[{nameof(VkPostFilter)}] Post won't be shown: bot is disabled.");
            return false;
        }

        if (DynamicSettings.SignalWords is null)
            return true;

        string? textLowercase = null;
        var signalWordFound = DynamicSettings.SignalWords.FirstOrDefault(x => Vk2TgConfig.Current.IgnoreSignalWordsCase
            ? (textLowercase ??= vkWallPost.Text.ToLowerInvariant()).Contains(x)
            : vkWallPost.Text.Contains(x))
        ?? (vkWallPost.Text.StartsWith(" ")
            ? "Unprinted symbol at the beginning of the text"
            : null);

        if (signalWordFound is null)
        {
            Logger.Info($"[{nameof(VkPostFilter)}] Post won't be shown: signal words not found.");
            return false;
        }

        Logger.Info($"[{nameof(VkPostFilter)}] Signal word found: '{signalWordFound}'. Showing post.");

        return true;
    }
}