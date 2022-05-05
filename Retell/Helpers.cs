using System.Text.RegularExpressions;
using NLog;
using Polly;
using Telegram.Bot.Exceptions;

namespace Retell;

public static class Helpers
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    
    private static readonly string VkLinkLong = new (@"\[https?:\/\/([^\]]+)\|([^\]]+)\]");
    private static readonly string VkLinkShort = new (@"\[(?!https?:\/\/)([^\]]+)\|([^\]]+)\]");
    private static readonly string VkLink = new (@"\[([^\]]+)\|([^\]]+)\]");
    
    public static readonly AsyncPolicy TelegramRetryForeverPolicy = Policy
        .Handle<Exception>(ex => ex is RequestException && ex.Message.ToLower().Contains("time") && ex.Message.ToLower().Contains("out"))
        .RetryForeverAsync(_ => Logger.Warn("Timeout. Retrying..."));

    public static bool TryTransformLinksVkToTelegram(string text, out string result)
    {
        result = text;
        if (!Regex.IsMatch(text, VkLink))
            return false;

        result = Regex.Replace(text, VkLinkShort, "<a href=\"vk.com/$1\">$2</a>");
        result = Regex.Replace(result, VkLinkLong, "<a href=\"$1\">$2</a>");
        return true;
    }

    public static string ToEscapedMarkdownString(this string s)
    {
        return s
            .Replace("_", "\\_")
            .Replace("*", "\\*")
            .Replace("[", "\\[")
            .Replace("`", "\\`");
    }

    public static string RemoveHashtags(this string s)
    {
        return Regex.Replace(s, @"#[^\s]+", string.Empty);
    }
}