using System;
using System.IO;
using System.Threading.Tasks;

namespace Vk2Tg
{
    public static class Program
    {
        public static async Task Main()
        {
            var config = await Vk2TgConfig.FromYaml(Path.Combine(Environment.CurrentDirectory, "config.yml"));
            var bot = new Vk2TgBot(config);
            await bot.Initialize();
            await bot.Run();
        }
    }
}


