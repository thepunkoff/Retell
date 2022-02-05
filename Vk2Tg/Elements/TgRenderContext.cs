using Telegram.Bot;
using Telegram.Bot.Types;

namespace Vk2Tg.Elements;

public class TgRenderContext
{
    public readonly TelegramBotClient BotClient;
    public readonly ChatId ChatId;
    public readonly HttpClient HttpClient;

    public TgRenderContext(TelegramBotClient botClient, ChatId chatId, HttpClient httpClient)
    {
        BotClient = botClient;
        ChatId = chatId;
        HttpClient = httpClient;
    }
}