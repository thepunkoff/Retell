using Microsoft.Extensions.Configuration;
using NLog;
using Vk2Tg.Configuration;
using VkNet.Model.GroupUpdate;

namespace Vk2Tg.Filtering;

public class VkPostFilterService
{
    private readonly IConfiguration _configuration;
    public VkPostFilterService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public FilteringResult Filter(WallPost vkWallPost)
    {
        if (!_configuration.GetSection("isBotEnabled").Get<bool>())
            return FilteringResult.BotDisabled;

        if (_configuration.GetSection("signalWords") is not {Value.Length: >0} signalWordsSection)
            return FilteringResult.ShouldShow;

        string? textLowercase = null;
        var signalWordFound = signalWordsSection.Get<string[]>().FirstOrDefault(x => _configuration.GetSection("ignoreSignalWordsCase").Get<bool>()
            ? (textLowercase ??= vkWallPost.Text.ToLowerInvariant()).Contains(x)
            : vkWallPost.Text.Contains(x))
        ?? (vkWallPost.Text.StartsWith(" ")
            ? "Unprinted symbol at the beginning of the text"
            : null);

        return signalWordFound is null ? FilteringResult.SignalWordsNotFound : FilteringResult.ShouldShow;

    }
}