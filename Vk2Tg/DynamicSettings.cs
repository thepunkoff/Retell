namespace Vk2Tg;

public static class DynamicSettings
{
    public static bool IsBotEnabled { get; set; } = true;

    public static string ToUserMarkdownString()
    {
        return $"Бот сейчас *{(IsBotEnabled ? "работает" : "не работает")}.*";
    }
}