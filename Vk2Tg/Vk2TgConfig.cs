using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Vk2Tg
{
    public class Vk2TgConfig
    {
        public static Vk2TgConfig Current { get; set; } = new ();
        public string VkToken { get; set; }
        public ulong VkGroupId { get; set; }
        public string TelegramToken { get; set; }
        public long TelegramChatId { get; set; }

        public string AdminPassword { get; set; }
        public GifMediaGroupMode GifMediaGroupMode { get; set; } = GifMediaGroupMode.Auto;

        public string GmailEmail { get; set; }
        
        public string GmailPassword { get; set; }

        public static async Task<Vk2TgConfig> FromYaml(string configPath)
        {
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(PascalCaseNamingConvention.Instance) 
                .Build();
            return deserializer.Deserialize<Vk2TgConfig>(await File.ReadAllTextAsync(configPath));
        }
    }
}