using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;
using Vk2Tg.Elements;
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
    public class Vk2TgBot
    {
        private readonly Vk2TgConfig _config;
        private readonly VkApi _vkApi;
        private readonly TelegramBotClient _tgBotClient;
        private readonly HttpClient _httpClient = new ()
        {
            Timeout = TimeSpan.FromSeconds(100)
        };

        private bool _initialized;
        private string? _key;
        private string? _server;
        private string? _ts;


        public Vk2TgBot(Vk2TgConfig config)
        {
            _config = config;
            var services = new ServiceCollection();
            services.AddAudioBypass();
            _vkApi = new VkApi(services);

            _tgBotClient = new TelegramBotClient(config.TelegramToken, _httpClient);
        }

        public async Task Initialize()
        {
            Console.WriteLine("Initializing Vk2Tg...");

            await _vkApi.AuthorizeAsync(new ApiAuthParams
            {
                AccessToken = _config.VkToken,
                Settings = Settings.All | Settings.Offline,
                TwoFactorAuthorization = Console.ReadLine,
            });

            var longPollServerResponse = await _vkApi.Groups.GetLongPollServerAsync(_config.VkGroupId);
            _key = longPollServerResponse.Key;
            _server = longPollServerResponse.Server;
            _ts = longPollServerResponse.Ts;
            
            Console.WriteLine("Initialization ok!");
            _initialized = true;
        }
        
        public async Task Run()
        {
            if (!_initialized)
                throw new InvalidOperationException($"You should first initialize bot via {nameof(Initialize)} method.");
            
            Console.WriteLine("Bot started.");
            while (true)
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
                    Console.WriteLine("LongPollOutdateException: using new ts.");
                    _ts = ex.Ts;
                    continue;
                }
                catch (LongPollKeyExpiredException)
                {
                    Console.WriteLine("LongPollKeyExpiredException: requesting key again.");
                    var longPollServerResponse = await _vkApi.Groups.GetLongPollServerAsync(_config.VkGroupId);
                    _key = longPollServerResponse.Key;
                    continue;
                }
                catch (LongPollInfoLostException)
                {
                    Console.WriteLine("LongPollInfoLostException: requesting key and ts again.");
                    var longPollServerResponse = await _vkApi.Groups.GetLongPollServerAsync(_config.VkGroupId);
                    _key = longPollServerResponse.Key;
                    _ts = longPollServerResponse.Ts;
                    continue;
                }
                catch (VkApiMethodInvokeException apiEx) when (apiEx.Message.ToLowerInvariant().Contains("unknown application"))
                {
                    Console.WriteLine($"Long poll history request failed. It happens - trying again. (error code: {apiEx.ErrorCode}):\n{apiEx}");
                    continue;
                }

                _ts = longPollResponse.Ts;

                foreach (var update in longPollResponse.Updates)
                {
                    if (update.WallPost is null)
                        continue;

                    if (update.WallPost.FromId is null)
                    {
                        Console.WriteLine("Error occured: no FromId!");
                        continue;
                    }

                    if (update.WallPost.FromId == -(long)_config.VkGroupId)
                        Console.WriteLine("New community wall post detected!");

                    var tgElement = CreateTgElement(update.WallPost);
                    await tgElement.Render(new TgRenderContext(_tgBotClient, _config.TelegramChatId, _httpClient), default);
                }
            }
        }

        private TgElement CreateTgElement(WallPost wallPost)
        {
            TgElement ret = new TgNullElement();

            if (!string.IsNullOrWhiteSpace(wallPost.Text))
                ret = ret.AddText(new TgText(wallPost.Text));

            foreach (var attachment in wallPost.Attachments)
            {
                switch (attachment.Instance)
                {
                    case Photo photo:
                        ret = ret.AddPhoto(new TgPhoto(photo.Sizes.Last().Url));
                        break;
                    case Video video:
                        var videoCollection = _vkApi.Video.Get(new VideoGetParams { Videos = new[] { video } });
                        ret = ret.AddVideo(new TgVideo(ChoseBestQualityUrl(videoCollection[0])));
                        break;
                    case Poll poll:
                        ret = ret.AddPoll(new TgPoll(poll.Question, poll.Answers.Select(x => x.Text).ToArray(), poll.Multiple ?? false));
                        break;
                    case Link link:
                        ret = ret.AddLink(new TgLink(link.Uri.ToString()));
                        break;
                    case Document doc:
                        if (doc.Ext == "gif")
                            ret = ret.AddGif(new TgGif(new Uri(doc.Uri)));
                        break;
                }
            }

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
    }
}