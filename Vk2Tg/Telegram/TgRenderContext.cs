using Telegram.Bot;
using Telegram.Bot.Types;

namespace Vk2Tg.Telegram;

public class TgRenderContext
{
    public readonly ITelegramBotClient BotClient;
    public readonly ChatId ChatId;
    public readonly HttpClient HttpClient;

    public TgRenderContext(ITelegramBotClient botClient, ChatId chatId, HttpClient httpClient)
    {
        BotClient = botClient;
        ChatId = chatId;
        HttpClient = httpClient;
    }
}