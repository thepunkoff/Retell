namespace Vk2Tg
{
    public static class Program
    {
        public static async Task Main()
        {
            Vk2TgConfig.Current = await Vk2TgConfig.FromYaml(Path.Combine(Environment.CurrentDirectory, "config.yml"));
            var bot = new Vk2TgBot();
            await bot.Initialize();
            await bot.Run();
        }
    }
}


