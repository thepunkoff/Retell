using Microsoft.Extensions.DependencyInjection;
using NLog;
using Telegram.Bot;
using Vk2Tg.Admin;
using Vk2Tg.Elements;
using Vk2Tg.Filtering;
using VkNet;
using VkNet.AudioBypassService.Extensions;
using VkNet.Enums.Filters;
using VkNet.Exception;
using VkNet.Model;
using VkNet.Model.Attachments;
using VkNet.Model.GroupUpdate;
using VkNet.Model.RequestParams;
using Poll = VkNet.Model.Attachments.Poll;
using Video = VkNet.Model.Attachments.Video;

namespace Vk2Tg
{
    public sealed class Vk2TgBot : IAsyncDisposable
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    
        private readonly Vk2TgConfig _config;
        private readonly VkApi _vkApi;
        private readonly TelegramBotClient _tgBotClient;
        private readonly AdminConsole _adminConsole;
        // private readonly HttpServer _httpServer = new (8080);
        private readonly HttpClient _httpClient = new ()
        {
            Timeout = TimeSpan.FromSeconds(100)
        };

        private bool _initialized;
        private string? _key;
        private string? _server;
        private string? _ts;


        public Vk2TgBot()
        {
            var services = new ServiceCollection();
            services.AddAudioBypass();
            _vkApi = new VkApi(services);

            _config = Vk2TgConfig.Current;
            _tgBotClient = new TelegramBotClient(_config.TelegramToken, _httpClient);
            _adminConsole = new AdminConsole(_tgBotClient);
        }

        public async Task Initialize()
        {
            Logger.Info("Initializing Vk2Tg...");

            Logger.Trace("Authorizing...");
            await _vkApi.AuthorizeAsync(new ApiAuthParams
            {
                AccessToken = _config.VkToken,
                Settings = Settings.All | Settings.Offline,
                TwoFactorAuthorization = Console.ReadLine,
            });
            Logger.Trace("Authorization ok.");

            Logger.Trace("Getting long poll server...");
            var longPollServerResponse = await _vkApi.Groups.GetLongPollServerAsync(_config.VkGroupId);
            _key = longPollServerResponse.Key;
            _server = longPollServerResponse.Server;
            _ts = longPollServerResponse.Ts;
            Logger.Trace("Get long poll server ok.");
            
            Logger.Trace("Initializing admin console...");
            _adminConsole.Start();
            Logger.Trace("Admin console initialized.");
            
            // Logger.Trace("Initializing http server...");
            // _httpServer.Start();
            // Logger.Trace("Http server initialized.");
            
            Logger.Info("Initialization ok.");
            _initialized = true;
        }
        
        public async Task Run()
        {
            if (!_initialized)
                throw new InvalidOperationException($"You should first initialize bot via {nameof(Initialize)} method.");

            Logger.Info("Bot started.");

            while (true)
            {
                try
                {
                    var longPollResponse = await GetNextLongPollResponse();
                    if (longPollResponse is not null)
                        await ProcessEvents(longPollResponse);
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Error occured while processing long poll responses.");
                    await MailService.SendException(ex);
                }

                await Task.Delay(TimeSpan.FromSeconds(1));
            }
        }

        private async Task ProcessEvents(BotsLongPollHistoryResponse longPollResponse)
        {
            foreach (var update in longPollResponse.Updates)
            {
                if (update.WallPost is null)
                    continue;

                if (update.WallPost.FromId is null)
                {
                    Logger.Warn("No FromId in the long poll response.");
                    continue;
                }

                if (update.WallPost.FromId != -(long)_config.VkGroupId)
                    continue;

                var shortPostString = update.WallPost.Text is not null
                    ? update.WallPost.Text.Length > 100
                        ? update.WallPost.Text[..100]
                        : update.WallPost.Text
                    : "no text in post";

                Logger.Info($"New community wall post detected: '{shortPostString}'");

                var filteringResult = VkPostFilter.Filter(update.WallPost);
                Logger.Info($"Filtering result: {filteringResult}");
                if (filteringResult is not FilteringResult.ShouldShow)
                    continue;

                var tgElement = CreateTgElement(update.WallPost);
                    
                Logger.Debug("Rendering TgElement...");
                await tgElement.Render(new TgRenderContext(_tgBotClient, _config.TelegramChatId, _httpClient), default);
                Logger.Debug("TgElement rendered.");
            }
        }

        private async Task<BotsLongPollHistoryResponse?> GetNextLongPollResponse()
        {
            BotsLongPollHistoryResponse longPollResponse;
            try
            {
                longPollResponse = await _vkApi.Groups.GetBotsLongPollHistoryAsync(new BotsLongPollHistoryParams
                {
                    Key = _key,
                    Server = _server,
                    Ts = _ts,
                });
            }
            catch (LongPollOutdateException ex)
            {
                Logger.Trace("LongPollOutdateException: using new ts.");
                _ts = ex.Ts;
                return null;
            }
            catch (LongPollKeyExpiredException)
            {
                Logger.Trace("LongPollKeyExpiredException: requesting key again.");
                var longPollServerResponse = await _vkApi.Groups.GetLongPollServerAsync(_config.VkGroupId);
                _key = longPollServerResponse.Key;
                return null;
            }
            catch (LongPollInfoLostException)
            {
                Logger.Trace("LongPollInfoLostException: requesting key and ts again.");
                var longPollServerResponse = await _vkApi.Groups.GetLongPollServerAsync(_config.VkGroupId);
                _key = longPollServerResponse.Key;
                _ts = longPollServerResponse.Ts;
                return null;
            }
            catch (VkApiMethodInvokeException apiEx) when (apiEx.Message.ToLowerInvariant().Contains("unknown application"))
            {
                Logger.Error($"Long poll history request failed. Trying again. (error code: {apiEx.ErrorCode}):\n{apiEx}");
                await MailService.SendException(apiEx);
                return null;
            }

            _ts = longPollResponse.Ts;

            return longPollResponse;
        }

        private TgElement CreateTgElement(WallPost wallPost)
        {
            Logger.Debug("Creating TgElement...");
            TgElement ret = new TgNullElement();

            if (!string.IsNullOrWhiteSpace(wallPost.Text))
            {
                var text = Vk2TgConfig.Current.ClearHashtags
                    ? wallPost.Text.RemoveHashtags()
                    : wallPost.Text;

                ret = ret.AddText(new TgText(text));
                Logger.Debug($"Added text.{(Vk2TgConfig.Current.ClearHashtags ? " Removed hashtags." : string.Empty)} Result: {ret}.");
            }

            foreach (var attachment in wallPost.Attachments)
            {
                switch (attachment.Instance)
                {
                    case Photo photo:
                        ret = ret.AddPhoto(new TgPhoto(photo.Sizes.Last().Url));
                        Logger.Debug($"Added photo. Result: {ret}.");
                        break;
                    case Video video:
                        var videoCollection = _vkApi.Video.Get(new VideoGetParams { Videos = new[] { video } });
                        ret = ret.AddVideo(new TgVideo(ChoseBestQualityUrl(videoCollection[0])));
                        Logger.Debug($"Added video. Result: {ret}.");
                        break;
                    case Poll poll:
                        ret = ret.AddPoll(new TgPoll(poll.Question, poll.Answers.Select(x => x.Text).ToArray(), poll.Multiple ?? false));
                        Logger.Debug($"Added poll. Result: {ret}.");
                        break;
                    case Link link:
                        ret = ret.AddLink(new TgLink(link.Uri.ToString()));
                        Logger.Debug($"Added link. Result: {ret}.");
                        break;
                    case Document doc:
                        if (doc.Ext == "gif")
                        {
                            ret = ret.AddGif(new TgGif(new Uri(doc.Uri)));
                            Logger.Debug($"Added gif. Result: {ret}.");
                        }
                        break;
                }
            }
            
            Logger.Debug("TgElement created.");
            return ret;
        }

        private static Uri ChoseBestQualityUrl(Video video)
        {
            if (video.Files is null)
                throw new ArgumentNullException(nameof(video), "The video files object was null.");

            if (video.Files.External is not null)
                return video.Files.External;
            if (video.Files.Mp4_1080 is not null)
                return video.Files.Mp4_1080;
            if (video.Files.Mp4_720 is not null)
                return video.Files.Mp4_720;
            if (video.Files.Mp4_480 is not null)
                return video.Files.Mp4_480;
            if (video.Files.Mp4_360 is not null)
                return video.Files.Mp4_360;
            if (video.Files.Mp4_240 is not null)
                return video.Files.Mp4_240;
            
            throw new ArgumentNullException(nameof(video), "All video files uris were null.");
        }

        public async ValueTask DisposeAsync()
        {
            // await _httpServer.DisposeAsync();
            await _adminConsole.DisposeAsync();
        }
    }
}