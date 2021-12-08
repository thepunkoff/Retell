using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using VkNet;
using VkNet.Enums.Filters;
using VkNet.Exception;
using VkNet.Model;
using VkNet.Model.Attachments;
using VkNet.Model.RequestParams;
using Poll = VkNet.Model.Attachments.Poll;
using Video = VkNet.Model.Attachments.Video;

namespace Vk2Tg
{
    public class Vk2TgBot
    {
        private readonly Config _config;
        private readonly VkApi _vkApi;
        private readonly TelegramBotClient _tgBotClient;
        private readonly HttpClient _httpClient = new ();

        private bool _initialized;
        private string? _key;
        private string? _server;
        private string? _ts;

        public Vk2TgBot(Config config)
        {
            _config = config;
            _vkApi = new VkApi();
            _tgBotClient = new TelegramBotClient(config.TelegramToken);
        }

        public async Task Initialize()
        {
            Console.WriteLine("Initializing Vk2Tg...");
            await _vkApi.AuthorizeAsync(new ApiAuthParams
            {
                AccessToken = _config.VkToken,
                Settings = Settings.All | Settings.Offline,
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

                    longPollResponse = await _vkApi.Groups.GetBotsLongPollHistoryAsync(new BotsLongPollHistoryParams()
                    {
                        Key = _key,
                        Server = _server,
                        Ts = _ts,
                    });

                }
                catch (VkApiMethodInvokeException apiEx) when (apiEx.Message.ToLowerInvariant().Contains("unknown application"))
                {
                    Console.WriteLine($"long poll history request failed. trying again. (error code: {apiEx.ErrorCode}):\n{apiEx}");
                    continue;
                }

                _ts = longPollResponse.Ts;

                foreach (var update in longPollResponse.Updates)
                {
                    if (update.WallPost is not null)
                    {
                        if (update.WallPost.FromId is null)
                        {
                            Console.WriteLine("Error occured: no FromId!");
                            continue;
                        }

                        if (update.WallPost.FromId == -(long)_config.VkGroupId)
                        {
                            Console.WriteLine("New community wall post detected!");
                        }

                        var text = update.WallPost.Text;
                        var mediaGroup = new List<IAlbumInputMedia>();
                        List<Stream> mediaStreams = new ();
                        foreach (var attachment in update.WallPost.Attachments)
                        {
                            if (attachment.Instance is Photo photo)
                            {
                                var stream = await _httpClient.GetStreamAsync(photo.Sizes.Last().Url);
                                mediaStreams.Add(stream);
                                mediaGroup.Add(new InputMediaPhoto(new InputMedia(stream, "image")));
                            }
                            
                            if (attachment.Instance is Video video)
                            {
                                
                            }
                            
                            if (attachment.Instance is Poll poll)
                            {
                                
                            }
                            
                            if (attachment.Instance is Link link)
                            {
                                
                            }
                        }

                        await _tgBotClient.SendMediaGroupAsync(_config.TelegramChatId, mediaGroup);
                        foreach (var stream in mediaStreams)
                        {
                            stream.Close();
                            await stream.DisposeAsync();
                        }
                    }
                }
            }
        }
    }
}