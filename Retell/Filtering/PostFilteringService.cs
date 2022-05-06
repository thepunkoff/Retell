using Microsoft.Extensions.Configuration;
using Retell.Abstractions.Services;
using Retell.Core.Models;

namespace Retell.Filtering;

public class PostFilteringService : IPostFilteringService
{
    private readonly IConfiguration _configuration;
    public PostFilteringService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public FilteringResult Filter(Post post)
    {
        if (!_configuration.GetSection("isBotEnabled").Get<bool>())
            return FilteringResult.BotDisabled;

        if (_configuration.GetSection("signalWords") is not {Value.Length: >0} signalWordsSection)
            return FilteringResult.ShouldShow;

        string? textLowercase = null;
        var signalWordFound = signalWordsSection.Get<string[]>().FirstOrDefault(x => _configuration.GetSection("ignoreSignalWordsCase").Get<bool>()
            ? (textLowercase ??= post.Text.ToLowerInvariant()).Contains(x)
            : post.Text.Contains(x))
        ?? (post.Text.StartsWith(" ")
            ? "Unprinted symbol at the beginning of the text"
            : null);

        return signalWordFound is null ? FilteringResult.SignalWordsNotFound : FilteringResult.ShouldShow;

    }
}