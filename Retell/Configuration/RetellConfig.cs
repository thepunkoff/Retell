using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
namespace Retell.Configuration
{
    public class RetellConfig
    {
        private static readonly ISerializer ConfigSerializer = new SerializerBuilder().WithNamingConvention(CamelCaseNamingConvention.Instance).Build();

        public Platform Source { get; set; }

        public Platform Destination { get; set; }

        public string AdminPassword { get; set; } = "admin";

        public int HttpPort { get; set; } = 8080;

        public bool IgnoreSignalWordsCase { get; set; }

        public bool ClearHashtags { get; set; }

        public int AutoLogoutIdlePeriodMinutes { get; set; }
        public GifMediaGroupMode GifMediaGroupMode { get; set; } = GifMediaGroupMode.Auto;

        public bool IsBotEnabled { get; set; }
#if DEBUG
            = true;
#endif

        public string[]? SignalWords { get; set; }

        public string ToUserMarkdownString()
        {
            return $"*Статус:*\n\nБот сейчас {(IsBotEnabled ? "*работает*. Чтобы его выключить, введите /disable" : "*выключен*. Чтобы его включить, введите /enable")}.\n\nСигнальные слова{(SignalWords is not null ? $": '{string.Join(", ", SignalWords).ToEscapedMarkdownString()}'. Регистр слов при проверке {(IgnoreSignalWordsCase ? "не учитывается" : "учитывается")}. Невидимый символ *в начале первой строки* (например, _&#013_;) является сигнальным словом по умолчанию. Чтобы выключить сигнальные слова, введите /disable\\_signal\\_words" : " *выключены*. Чтобы установить сигнальные слова, введите /signal <слово> <слово> <слово> ... (без угловых скобок)")}.";
        }
        public void Save()
        {
            using var writer = File.CreateText(Path.Combine(Environment.CurrentDirectory, "config.yml"));
            ConfigSerializer.Serialize(writer, this);
        }
    }
}