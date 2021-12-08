using System.IO;
using System.Threading.Tasks;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Vk2Tg
{
    public class Config
    {
        public string VkToken { get; set; }
        public ulong VkGroupId { get; set; }
        public string TelegramToken { get; set; }
        public long TelegramChatId { get; set; }

        public static async Task<Config> FromYaml(string configPath)
        {
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(PascalCaseNamingConvention.Instance) 
                .Build();
            return deserializer.Deserialize<Config>(await File.ReadAllTextAsync(configPath));
        }
    }
}