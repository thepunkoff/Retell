namespace Vk2Tg;

public static class DynamicSettings
{
    public static bool IsBotEnabled { get; set; }

    public static string[]? SignalWords { get; private set; }

    public static void SetSignalWords(string[] signalWords)
    {
        SignalWords = Vk2TgConfig.Current.IgnoreSignalWordsCase
            ? signalWords.Select(x => x.ToLowerInvariant()).ToArray()
            : signalWords;
    }

    public static void DisableSignalWords()
    {
        SignalWords = null;
    }
    
    public static string ToUserMarkdownString()
    {
        return $"*Статус:*\n\nБот сейчас {(IsBotEnabled ? "*работает*. Чтобы его выключить, введите /disable" : "*выключен*. Чтобы его включить, введите /enable")}.\n\nСигнальные слова{(SignalWords is not null ? $": '{string.Join(", ", SignalWords).ToEscapedMarkdownString()}'. Регистр слов при проверке {(Vk2TgConfig.Current.IgnoreSignalWordsCase ? "не учитывается" : "учитывается")}. Невидимый символ *в начале первой строки* (например, _&#013_;) является сигнальным словом по умолчанию. Чтобы выключить сигнальные слова, введите /disable\\_signal\\_words" : " *выключены*. Чтобы установить сигнальные слова, введите /signal <слово> <слово> <слово> ... (без угловых скобок)")}.";
    }
}