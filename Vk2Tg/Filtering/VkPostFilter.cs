using NLog;
using VkNet.Model.GroupUpdate;

namespace Vk2Tg.Filtering;

public static class VkPostFilter
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public static FilteringResult Filter(WallPost vkWallPost)
    {
        if (!DynamicSettings.IsBotEnabled)
            return FilteringResult.BotDisabled;

        if (DynamicSettings.SignalWords is null)
            return FilteringResult.ShouldShow;

        string? textLowercase = null;
        var signalWordFound = DynamicSettings.SignalWords.FirstOrDefault(x => Vk2TgConfig.Current.IgnoreSignalWordsCase
            ? (textLowercase ??= vkWallPost.Text.ToLowerInvariant()).Contains(x)
            : vkWallPost.Text.Contains(x))
        ?? (vkWallPost.Text.StartsWith(" ")
            ? "Unprinted symbol at the beginning of the text"
            : null);

        if (signalWordFound is null)
            return FilteringResult.SignalWordsNotFound;

        return FilteringResult.ShouldShow;
    }
}