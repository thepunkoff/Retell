using NLog;
using Telegram.Bot;
using Telegram.Bot.Extensions.Polling;
using Telegram.Bot.Types.Enums;

namespace Vk2Tg.Admin;

public sealed class AdminConsole : IAsyncDisposable
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private readonly ITelegramBotClient _telegramBotClient;
    private readonly QueuedUpdateReceiver _updateReceiver;
    private readonly CancellationTokenSource _cts = new();
    private readonly List<long> _authorizedIds = new();

    private Task? _worker;

    public AdminConsole(ITelegramBotClient telegramBotClient)
    {
        _telegramBotClient = telegramBotClient;
        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = new [] { UpdateType.Message },
        };
        _updateReceiver = new QueuedUpdateReceiver(telegramBotClient, receiverOptions);
    }

    public void Start()
    {
        _worker = Task.Run(async () =>
        {
            try
            {

                await foreach (var update in _updateReceiver.WithCancellation(_cts.Token))
                {
                    if (update.Message is not { } message)
                        continue;

                    if (message.From is null)
                    {
                        Logger.Error("Message.From was null.");
                        continue;
                    }

                    if (message.Text is null)
                    {
                        Logger.Error("Message.Text was null.");
                        await _telegramBotClient.SendTextMessageAsync(message.From.Id, "Error: message text is null.");
                        continue;
                    }

                    Logger.Trace($"Incoming command: '{(message.Text.Length > 100 ? message.Text[..100] : message.Text)}'.");

                    var split = message.Text.Split(" ");
                    switch (split[0])
                    {
                        case "/start":
                            const string helpMessage = "/enable - Включить бота\n/disable - Выключить бота";
                            await _telegramBotClient.SendTextMessageAsync(message.From.Id,
                                (!_authorizedIds.Contains(message.From.Id)
                                    ? "Чтобы авторизоваться, введите '/login <ваш пароль>' (без угловых скобок).\n\n"
                                    : string.Empty) + helpMessage);
                            continue;
                        case "/status":
                            if (!await CheckAuth(message.From.Id))
                                continue;
                            Logger.Trace($"[{nameof(AdminConsole)}] Sending dynamic settings to admin.");
                            await _telegramBotClient.SendTextMessageAsync(message.From.Id, DynamicSettings.ToUserMarkdownString(), ParseMode.Markdown);
                            break;
                        case "/login":
                            if (split.Length == 1)
                            {
                                Logger.Trace($"[{nameof(AdminConsole)}] Login command contained of only one part. Couldn't find password in the string.");
                                await _telegramBotClient.SendTextMessageAsync(message.From.Id, "Чтобы авторизоваться, введите '/login <ваш пароль>' (без угловых скобок).");
                                continue;
                            }

                            var password = split[1];
                            Logger.Trace($"[{nameof(AdminConsole)}] Deleting password string.");
                            await _telegramBotClient.DeleteMessageAsync(message.From.Id, message.MessageId);
                            Logger.Trace($"[{nameof(AdminConsole)}] Sending password string deletion warning.");
                            await _telegramBotClient.SendTextMessageAsync(message.From.Id, "_Мы удалили сообщение с вашим паролем для безопасности._", ParseMode.Markdown);
                            await Authorize(message.From.Id, password);
                            break;
                        case "/enable" when DynamicSettings.IsBotEnabled:
                            if (!await CheckAuth(message.From.Id))
                                continue;
                            Logger.Info($"[{nameof(AdminConsole)}] Bot already enabled.");
                            await _telegramBotClient.SendTextMessageAsync(message.From.Id, "Бот уже работает.");
                            break;
                        case "/enable":
                            if (!await CheckAuth(message.From.Id))
                                continue;
                            Logger.Info($"[{nameof(AdminConsole)}] Enabling bot.");
                            DynamicSettings.IsBotEnabled = true;
                            await _telegramBotClient.SendTextMessageAsync(message.From.Id, "*Бот запущен.*", ParseMode.Markdown);
                            break;
                        case "/disable" when !DynamicSettings.IsBotEnabled:
                            if (!await CheckAuth(message.From.Id))
                                continue;
                            Logger.Info($"[{nameof(AdminConsole)}] Bot already disabled.");
                            await _telegramBotClient.SendTextMessageAsync(message.From.Id, "Бот и так выключен.");
                            break;
                        case "/disable":
                            if (!await CheckAuth(message.From.Id))
                                continue;
                            Logger.Info($"[{nameof(AdminConsole)}] Disabling bot.");
                            DynamicSettings.IsBotEnabled = false;
                            await _telegramBotClient.SendTextMessageAsync(message.From.Id, "*Бот выключен.*", ParseMode.Markdown);
                            break;
                        default:
                            Logger.Info($"[{nameof(AdminConsole)}] Command don't exist.");
                            await _telegramBotClient.SendTextMessageAsync(message.From.Id,
                                "Такой команды не существует.");
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "AdminConsole worker creshed. Please, restart the bot.");
            }

        }, _cts.Token);
    }

    private async Task Authorize(long userId, string password)
    {
        if (_authorizedIds.Contains(userId))
        {
            Logger.Info($"[{nameof(AdminConsole)}] Already authorized: {userId}.");
            await _telegramBotClient.SendTextMessageAsync(userId, "Вы уже авторизованы.");
            return;
        }
        
        if (password != Vk2TgConfig.Current.AdminPassword)
        {
            Logger.Info($"[{nameof(AdminConsole)}] Wrong password: {userId}.");
            await _telegramBotClient.SendTextMessageAsync(userId, "Неверный пароль.");
            return;
        }
        
        _authorizedIds.Add(userId);
        Logger.Info($"[{nameof(AdminConsole)}] Successful authorization: {userId}.");
        await _telegramBotClient.SendTextMessageAsync(userId, $"*Вы успешно авторизованы!*\n\n{DynamicSettings.ToUserMarkdownString()}", ParseMode.Markdown);
    }

    private async Task<bool> CheckAuth(long userId)
    {
        if (_authorizedIds.Contains(userId))
            return true;

        Logger.Info($"[{nameof(AdminConsole)}] Unauthorized user.");
        await _telegramBotClient.SendTextMessageAsync(userId, "Вы не авторизованы. Авторизуйтель, отправив '/login <пароль>' (без угловых скобок).");
        return false;
    }

    public async ValueTask DisposeAsync()
    {
        if (_worker is not null)
            await _worker;
    }
}