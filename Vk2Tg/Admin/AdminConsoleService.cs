using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Extensions.Polling;
using Telegram.Bot.Types.Enums;
using Vk2Tg.Configuration;
using Vk2Tg.Services;

namespace Vk2Tg.Admin;

public sealed class AdminConsoleService : BackgroundService
{
    private readonly ITelegramBotClient _telegramBotClient;
    private readonly ILogger<AdminConsoleService> _logger;
    private readonly IExceptionReportService _reportService;
    private readonly IConfiguration _configuration;
    private readonly QueuedUpdateReceiver _updateReceiver;
    private readonly List<long> _authorizedIds = new();
    private DateTime _lastMessageTimestamp = DateTime.MinValue;

    public AdminConsoleService(ITelegramBotClient telegramBotClient, 
        ILogger<AdminConsoleService> logger, 
        IExceptionReportService reportService,
        IConfiguration configuration)
    {
        _telegramBotClient = telegramBotClient;
        _logger = logger;
        _reportService = reportService;
        _configuration = configuration;
        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = new [] { UpdateType.Message },
        };
        _updateReceiver = new QueuedUpdateReceiver(telegramBotClient, receiverOptions);
    }

    private async Task Authorize(long userId, string password)
    {
        if (_authorizedIds.Contains(userId))
        {
            _logger.LogInformation("Already authorized: {UserId}", userId);
            await _telegramBotClient.SendTextMessageAsync(userId, "Вы уже авторизованы.");
            return;
        }
        
        if (password != _configuration.GetSection("adminPassword").Value)
        {
            _logger.LogInformation("Wrong password: {UserId}", userId);
            await _telegramBotClient.SendTextMessageAsync(userId, "Неверный пароль.");
            return;
        }
        
        _authorizedIds.Add(userId);
        _logger.LogInformation("Successful authorization: {UserId}", userId);
        var logoutTimeMinutes = _configuration.GetSection("autoLogoutIdlePeriodMinutes").Get<int>();
        await _telegramBotClient.SendTextMessageAsync(userId, $"*Вы успешно авторизованы!*\n\n*Внимание!* Вы будете автоматически разлогинены через *{logoutTimeMinutes}* минут бездействия.", ParseMode.Markdown);
        await _telegramBotClient.SendTextMessageAsync(userId, _configuration.Get<Vk2TgConfig>().ToUserMarkdownString(), ParseMode.Markdown);
    }

    private async Task<bool> CheckAuth(long userId)
    {
        if (_authorizedIds.Contains(userId))
            return true;

        _logger.LogInformation("Unauthorized user");
        await _telegramBotClient.SendTextMessageAsync(userId, "Вы не авторизованы. Авторизуйтель, отправив '/login <пароль>' (без угловых скобок).");
        return false;
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var update in _updateReceiver.WithCancellation(stoppingToken))
        {
            if (_lastMessageTimestamp != DateTime.UtcNow &&
                DateTime.UtcNow - _lastMessageTimestamp >
                TimeSpan.FromMinutes(_configuration.GetSection("autoLogoutIdlePeriodMinutes").Get<int>()) && _authorizedIds.Count > 0)
            {
                _authorizedIds.Clear();
                _logger.LogInformation("Automatic logout triggered. All authorized users are no longer authorized");
            }

            _lastMessageTimestamp = DateTime.UtcNow;

            if (update.Message is not { } message)
                continue;

            if (message.From is null)
            {
                _logger.LogError("Message.From is null");
                continue;
            }

            try
            {

                if (message.Text is null)
                {
                    _logger.LogError("Message.Text is null");
                    await _telegramBotClient.SendTextMessageAsync(message.From.Id, "Error: message text is null.", cancellationToken: stoppingToken);
                    continue;
                }

                _logger.LogTrace("Incoming command: '{Text}'", message.Text.Length > 100 ? message.Text[..100] : message.Text);

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
                                : string.Empty) + helpMessageMarkdown, ParseMode.Markdown, cancellationToken: stoppingToken);
                        continue;
                    case "/status":
                        if (!await CheckAuth(message.From.Id))
                            continue;
                        _logger.LogTrace("Sending dynamic settings to admin");
                        await _telegramBotClient.SendTextMessageAsync(message.From.Id, _configuration.Get<Vk2TgConfig>().ToUserMarkdownString(), ParseMode.Markdown, cancellationToken: stoppingToken);
                        break;
                    case "/login":
                        if (split.Length == 1)
                        {
                            _logger.LogTrace("Login command contained of only one part. Couldn't find password in the string");
                            await _telegramBotClient.SendTextMessageAsync(message.From.Id,
                                "Чтобы авторизоваться, введите '/login <ваш пароль>' (без угловых скобок).", cancellationToken: stoppingToken);
                            continue;
                        }

                        var password = split[1];
                        _logger.LogTrace("Deleting password string");
                        await _telegramBotClient.DeleteMessageAsync(message.From.Id, message.MessageId, cancellationToken: stoppingToken);
                        _logger.LogTrace("Sending password string deletion warning");
                        await _telegramBotClient.SendTextMessageAsync(message.From.Id,
                            "_Мы удалили сообщение с вашим паролем для безопасности._", ParseMode.Markdown, cancellationToken: stoppingToken);
                        await Authorize(message.From.Id, password);
                        break;
                    case "/signal":
                        if (!await CheckAuth(message.From.Id))
                            continue;
                        var signalWordsSection = _configuration.GetSection("signalWords");
                        if (split.Length == 1)
                        {
                            _logger.LogTrace("Signal command should contain at least 1 argument");
                            await _telegramBotClient.SendTextMessageAsync(message.From.Id,
                                $"Текущие сигнальные слова: '{(signalWordsSection is { } ? string.Join(", ", signalWordsSection.Get<string[]>()) : " выключены")}'.\n\nЧтобы установить сигнальные слова укажите хотя бы одно слово после команды /signal. Предыдущие сигнальные слова перезатрутся.\n\nНевидимый символ *в начале первой строки* (например, _&#013_;) является сигнальным словом по умолчанию.", cancellationToken: stoppingToken);
                            continue;
                        }

                        _logger.LogTrace("Setting signal words...");
                        var signalWords = split.Skip(1).ToArray();
                        var cfg = _configuration.Get<Vk2TgConfig>();
                        cfg.SignalWords = signalWords;
                        cfg.Save();
                        var currentSignalWordsString = string.Join(", ", signalWordsSection.Get<string[]>());
                        _logger.LogTrace("Signal words set. Current: '{CurrentSignalWordsString}'", currentSignalWordsString);
                        await _telegramBotClient.SendTextMessageAsync(message.From.Id, $"*Сигнальные слова установлены.* Текущие сигнальные слова: '{currentSignalWordsString.ToEscapedMarkdownString()}'.\n\nРегистр слов при проверке {(_configuration.GetSection("ignoreSignalWordsCase").Get<bool>() ? "не учитывается" : "учитывается")}.\n\nНевидимый символ *в начале первой строки* (например, _&#013_;) является сигнальным словом по умолчанию.{(!cfg.IsBotEnabled ? "\n\nНе забывайте, что бот сейчас выключен. Чтобы его включить, введите /enable." : string.Empty)}", ParseMode.Markdown, cancellationToken: stoppingToken);
                        break;
                    case "/disable_signal_words":
                        if (!await CheckAuth(message.From.Id))
                            continue;
                        _logger.LogTrace("Disabling signal words...");
                        var cfg1 = _configuration.Get<Vk2TgConfig>();
                        cfg1.SignalWords = null;
                        cfg1.Save();
                        _logger.LogTrace("Signal words disabled");
                        await _telegramBotClient.SendTextMessageAsync(message.From.Id,
                            $"*Сигнальные слова выключены.* Теперь все посты будут репоститься. Чтобы установить сигнальные слова, введите /signal <слово> <слово> <слово> ... (без угловых скобок){(!cfg1.IsBotEnabled ? "\n\nНе забывайте, что бот сейчас выключен. Чтобы его включить, введите /enable." : string.Empty)}",
                            ParseMode.Markdown, cancellationToken: stoppingToken);
                        break;
                    case "/enable" when _configuration.GetSection("isBotEnabled").Get<bool>():
                        if (!await CheckAuth(message.From.Id))
                            continue;
                        _logger.LogInformation("Bot already enabled");
                        await _telegramBotClient.SendTextMessageAsync(message.From.Id, "Бот уже работает.", cancellationToken: stoppingToken);
                        break;
                    case "/enable":
                        if (!await CheckAuth(message.From.Id))
                            continue;
                        _logger.LogInformation("Enabling bot");
                        var cfg2 = _configuration.Get<Vk2TgConfig>();
                        cfg2.IsBotEnabled = true;
                        cfg2.Save();
                        await _telegramBotClient.SendTextMessageAsync(message.From.Id, "*Бот запущен.*", ParseMode.Markdown, cancellationToken: stoppingToken);
                        await _telegramBotClient.SendTextMessageAsync(message.From.Id, cfg2.ToUserMarkdownString(), ParseMode.Markdown, cancellationToken: stoppingToken);
                        break;
                    case "/disable" when !_configuration.GetSection("isBotEnabled").Get<bool>():
                        if (!await CheckAuth(message.From.Id))
                            continue;
                        _logger.LogInformation("Bot already disabled");
                        await _telegramBotClient.SendTextMessageAsync(message.From.Id, "Бот и так выключен.", cancellationToken: stoppingToken);
                        break;
                    case "/disable":
                        if (!await CheckAuth(message.From.Id))
                            continue;
                        _logger.LogInformation("Disabling bot");
                        var cfg3 = _configuration.Get<Vk2TgConfig>();
                        cfg3.IsBotEnabled = false;
                        cfg3.Save();
                        await _telegramBotClient.SendTextMessageAsync(message.From.Id, "*Бот выключен.*",
                            ParseMode.Markdown, cancellationToken: stoppingToken);
                        break;
                    default:
                        _logger.LogInformation("Command don't exist");
                        await _telegramBotClient.SendTextMessageAsync(message.From.Id,
                            "Такой команды не существует.", cancellationToken: stoppingToken);
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occured in handler");
                
                await _telegramBotClient.SendTextMessageAsync(message.From.Id, $"Произошла ошибка. Сообщите о ней администратору.\n\nДетали:\n{ex}", cancellationToken: stoppingToken);
                await _reportService.SendExceptionAsync(ex);
            }
        }
    }
}