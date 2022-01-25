using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Vk2Tg
{
    public class Vk2TgConfig
    {
        public string VkToken { get; set; }
        public ulong VkGroupId { get; set; }
        public string TelegramToken { get; set; }
        public long TelegramChatId { get; set; }

        public static async Task<Vk2TgConfig> FromYaml(string configPath)
        {
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(PascalCaseNamingConvention.Instance) 
                .Build();
            return deserializer.Deserialize<Vk2TgConfig>(await File.ReadAllTextAsync(configPath));
        }
    }
}