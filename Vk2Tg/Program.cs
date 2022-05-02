﻿using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using Telegram.Bot;
using Vk2Tg;
using Vk2Tg.Abstractions.Services;
using Vk2Tg.Admin;
using Vk2Tg.Configuration;
using Vk2Tg.Core;
using Vk2Tg.Filtering;
using Vk2Tg.Http.Handlers;
using Vk2Tg.Services;
using Vk2Tg.Telegram;
using Vk2Tg.Vk;
using VkNet;
using VkNet.Abstractions;
using VkNet.AudioBypassService.Extensions;

var builder = Host.CreateDefaultBuilder(args);

builder.ConfigureAppConfiguration(configurationBuilder =>
{
    configurationBuilder.Sources.Clear();
    configurationBuilder.Properties.Clear();

    var path = Path.Combine(Environment.CurrentDirectory, "config.yml");

    if (!File.Exists(path))
        new Vk2TgConfig().Save();

    configurationBuilder.AddYamlFile(path, false, true);
    configurationBuilder.AddEnvironmentVariables("Vk2Tg_");
});

builder.ConfigureLogging(loggingBuilder =>
{
    loggingBuilder.ClearProviders();
    loggingBuilder.SetMinimumLevel(LogLevel.Trace);
    loggingBuilder.AddNLog();
});

builder.ConfigureServices(collection =>
{
    // Common
    collection.AddSingleton(_ => new HttpClient
    {
        Timeout = TimeSpan.FromSeconds(100)
    });

#if DEBUG
    collection.AddSingleton<IExceptionReportService, NullExceptionReportService>();
#else
    collection.AddSingleton<IExceptionReportService, MailExceptionReportService>();
    collection.AddSingleton(_ => new SmtpClient("smtp.gmail.com")
    {
        EnableSsl = true,
        Port = 587,
        UseDefaultCredentials = false,
    });
#endif

    collection.AddSingleton<VkPostFilterService>();
    collection.AddSingleton<SettingsHandlerService>();

    // Platform specific
    var serviceProvider = collection.BuildServiceProvider();
    var configuration = serviceProvider.GetService<IConfiguration>();
    var source = configuration.GetValue<Platform>("source");
    var destination = configuration.GetValue<Platform>("destination");

    if (source is Platform.Vk)
    {
        collection.AddSingleton<IVkApi>(new VkApi(collection));
        collection.AddAudioBypass();
        collection.AddSingleton<IPostSource, VkPostSource>();
    }
    else
    {
        throw new NotSupportedException("Only vk.com is supported as a source platform.");
    }

    if (destination is Platform.Telegram)
    {
        collection.AddSingleton<ITelegramBotClient>(provider => new TelegramBotClient(
            provider.GetRequiredService<IConfiguration>().Get<TgSecrets>().TelegramToken,
            provider.GetRequiredService<HttpClient>()));
        collection.AddSingleton<IPostRenderer, TelegramPostRenderer>();
    }
    else
    {
        throw new NotSupportedException("Only Telegram is supported as a destination platform.");
    }

    // Services
    collection.AddHostedService<BotService>();
    collection.AddHostedService<AdminConsoleService>();
    // collection.AddHostedService<HttpServerService>();
});

await builder.Build().RunAsync();