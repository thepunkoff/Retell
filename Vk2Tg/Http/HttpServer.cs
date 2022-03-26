using System.Net;
using NLog;
using Vk2Tg.Http.Handlers;

namespace Vk2Tg.Http
{
    public class HttpServer : IAsyncDisposable
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private const string Http = "http";

        private readonly int _port;
        private readonly HttpListener _httpListener = new();
        private readonly CancellationTokenSource _cts = new();

        private readonly SettingsHandler _settingsHandler;

        private Task? _serverWorker;

        public HttpServer(int port)
        {
            _port = port;
            _settingsHandler = new SettingsHandler();
        }
        
        public void Start()
        {
            _httpListener.Prefixes.Add($"http://*:{_port}/");
            _httpListener.Start();

            _serverWorker = Task.Run(ServerWorker, _cts.Token);

            Logger.Info($"[{Http}] started listening http requests on port {_port}");
        }

        private async Task ServerWorker()
        {
            try
            {
                while (!_cts.IsCancellationRequested)
                {
                    var context = await _httpListener.GetContextAsync();
                    var request = context.Request;

                    if (request.HttpMethod != "GET")
                    {
                        await context.Response.ReturnBadRequest("This endpoint supports only GET requests.");
                        continue;
                    }

                    if (request.Url is null)
                    {
                        Logger.Warn($"[{Http}] incoming request url was null.");
                        continue;
                    }
                    
                    if (request.Url.AbsolutePath != "/favicon.ico")
                        Logger.Info($"[{Http}] incoming request: '{request.Url}' from {request.RemoteEndPoint}");

                    switch (request.Url.AbsolutePath)
                    {
                        case "/favicon.ico":
                        {
                            await context.Response.ReturnOk();
                            break;
                        }
                        case "/status":
                        {
                            Logger.Trace($"[{Http}] Procesing status request...");
                            await context.Response.ReturnOk();
                            Logger.Trace($"[{Http}] Status request processed. Ok.");
                            break;
                        }
                        case "/settings":
                        {
                            await _settingsHandler.HandleSettingsRequest(context);
                            break;
                        }
                        default:
                        {
                            Logger.Trace($"[{Http}] raw url was '{request.RawUrl}'. responding 400 bad request.");
                            await context.Response.ReturnBadRequest($"Endpoint '{request.Url.AbsolutePath}' not found. Try '/status' or '/settings'.");
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"[{Http}] {nameof(ServerWorker)} crashed:\n{ex}");
            }
        }

        public async ValueTask DisposeAsync()
        {
            _cts.Cancel();
            if (_serverWorker is not null)
                await _serverWorker;
            _httpListener.Stop();
            Logger.Trace($"[{Http}] stopped http server on port 8080.");
            _cts.Dispose();
        }
    }
}