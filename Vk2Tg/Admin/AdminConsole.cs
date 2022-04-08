using NLog;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
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
    private DateTime _lastMessageTimestamp = DateTime.MinValue;

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
            await foreach (var update in _updateReceiver.WithCancellation(_cts.Token))
            {
                if (_lastMessageTimestamp != DateTime.UtcNow &&
                    DateTime.UtcNow - _lastMessageTimestamp >
                    TimeSpan.FromMinutes(Vk2TgConfig.Current.AutoLogoutIdlePeriodMinutes) && _authorizedIds.Count > 0)
                {
                    _authorizedIds.Clear();
                    Logger.Info(
                        $"[{nameof(AdminConsole)}] Automatic logout triggered. All authorized users are no longer authorized.");
                }

                _lastMessageTimestamp = DateTime.UtcNow;

                if (update.Message is not { } message)
                    continue;

                if (message.From is null)
                {
                    Logger.Error("Message.From was null.");
                    continue;
                }

                try
                {

                    if (message.Text is null)
                    {
                        Logger.Error("Message.Text was null.");
                        await _telegramBotClient.SendTextMessageAsync(message.From.Id, "Error: message text is null.");
                        continue;
                    }

                    Logger.Trace(
                        $"Incoming command: '{(message.Text.Length > 100 ? message.Text[..100] : message.Text)}'.");

                    var split = message.Text.Split(" ");
                    switch (split[0])
                    {
                        case "/start":
                            const string helpMessageMarkdown =
                                "/status - Отобразить статус бота и текущие настройки\n" +
                                "/enable - Включить бота\n" +
                                "/disable - Выключить бота\n" +
                                "/signal <слово> <слово> <слово> ... - Установить сигнальные слова (без угловых скобок)\n" +
                                "/disable\\_signal\\_words - Выключить сигнальные слова";
                            await _telegramBotClient.SendTextMessageAsync(message.From.Id,
                                (!_authorizedIds.Contains(message.From.Id)
                                    ? "*Вы не авторизованы.* Чтобы авторизоваться, введите '/login <ваш пароль>' (без угловых скобок).\n\n"
                                    : string.Empty) + helpMessageMarkdown, ParseMode.Markdown);
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
                                Logger.Trace(
                                    $"[{nameof(AdminConsole)}] Login command contained of only one part. Couldn't find password in the string.");
                                await _telegramBotClient.SendTextMessageAsync(message.From.Id,
                                    "Чтобы авторизоваться, введите '/login <ваш пароль>' (без угловых скобок).");
                                continue;
                            }

                            var password = split[1];
                            Logger.Trace($"[{nameof(AdminConsole)}] Deleting password string.");
                            await _telegramBotClient.DeleteMessageAsync(message.From.Id, message.MessageId);
                            Logger.Trace($"[{nameof(AdminConsole)}] Sending password string deletion warning.");
                            await _telegramBotClient.SendTextMessageAsync(message.From.Id,
                                "_Мы удалили сообщение с вашим паролем для безопасности._", ParseMode.Markdown);
                            await Authorize(message.From.Id, password);
                            break;
                        case "/signal":
                            if (split.Length == 1)
                            {
                                Logger.Trace(
                                    $"[{nameof(AdminConsole)}] signal command should contain at least 1 argument.");
                                await _telegramBotClient.SendTextMessageAsync(message.From.Id,
                                    $"Текущие сигнальные слова: '{(DynamicSettings.SignalWords is not null ? string.Join(", ", DynamicSettings.SignalWords) : " выключены")}'.\n\nЧтобы установить сигнальные слова укажите хотя бы одно слово после команды /signal. Предыдущие сигнальные слова перезатрутся.\n\nНевидимый символ *в начале строки* (например, _&#013_;) является сигнальным словом по умолчанию.");
                                continue;
                            }

                            Logger.Trace($"[{nameof(AdminConsole)}] Setting signal words...");
                            var signalWords = split.Skip(1).ToArray();
                            DynamicSettings.SetSignalWords(signalWords);
                            var currentSignalWordsString = string.Join(", ", DynamicSettings.SignalWords!);
                            Logger.Trace($"[{nameof(AdminConsole)}] Signal words set. Current: '{currentSignalWordsString}'.");
                            await _telegramBotClient.SendTextMessageAsync(message.From.Id, $"*Сигнальные слова установлены.* Текущие сигнальные слова: '{currentSignalWordsString.ToEscapedMarkdownString()}'.\n\nНевидимый символ *в начале строки* (например, _&#013_;) является сигнальным словом по умолчанию.{(!DynamicSettings.IsBotEnabled ? "\n\nНе забывайте, что бот сейчас выключен. Чтобы его включить, введите /enable." : string.Empty)}", ParseMode.Markdown);
                            break;
                        case "/disable_signal_words":
                            Logger.Trace($"[{nameof(AdminConsole)}] Disabling signal words...");
                            DynamicSettings.DisableSignalWords();
                            Logger.Trace($"[{nameof(AdminConsole)}] Signal words disabled.");
                            await _telegramBotClient.SendTextMessageAsync(message.From.Id,
                                $"*Сигнальные слова выключены.* Теперь все посты будут репоститься. Чтобы установить сигнальные слова, введите /signal <слово> <слово> <слово> ... (без угловых скобок){(!DynamicSettings.IsBotEnabled ? "\n\nНе забывайте, что бот сейчас выключен. Чтобы его включить, введите /enable." : string.Empty)}",
                                ParseMode.Markdown);
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
                            await _telegramBotClient.SendTextMessageAsync(message.From.Id, DynamicSettings.ToUserMarkdownString(), ParseMode.Markdown);
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
                            await _telegramBotClient.SendTextMessageAsync(message.From.Id, "*Бот выключен.*",
                                ParseMode.Markdown);
                            break;
                        default:
                            Logger.Info($"[{nameof(AdminConsole)}] Command don't exist.");
                            await _telegramBotClient.SendTextMessageAsync(message.From.Id,
                                "Такой команды не существует.");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "AdminConsole error occured.");
                    await MailService.SendException(ex);
                    await _telegramBotClient.SendTextMessageAsync(message.From.Id, $"Произошла ошибка. Сообщите о ней администратору.\n\nДетали:\n{ex}");
                }
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
        await _telegramBotClient.SendTextMessageAsync(userId, $"*Вы успешно авторизованы!*\n\n*Внимание!* Вы будете автоматически разлогинены через *{Vk2TgConfig.Current.AutoLogoutIdlePeriodMinutes}* минут бездействия.", ParseMode.Markdown);
        await _telegramBotClient.SendTextMessageAsync(userId, DynamicSettings.ToUserMarkdownString(), ParseMode.Markdown);
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