using DstQueryBot.Models;
using DstQueryBot.Services;
using EleCho.GoCqHttpSdk;
using EleCho.GoCqHttpSdk.Message;
using EleCho.GoCqHttpSdk.MessageMatching;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;

internal class Program
{
    static void Main()
    {
        var builder = Host.CreateApplicationBuilder();

        builder.Configuration.AddJsonFile("appsettings.json", true);
        builder.Configuration.AddYamlFile("appsettings.yaml", true);
        builder.Configuration.AddUserSecrets(typeof(Program).Assembly);

        builder.Services.AddSerilog(configure =>
        {
            configure.WriteTo.Console();
        });

#pragma warning disable IL2026
#pragma warning disable IL3050
        builder.Services.AddSingleton(builder.Configuration.GetSection("DstConfig").Get<DstConfig>() ?? new());
        builder.Services.AddSingleton(builder.Configuration.GetSection("OneBot").Get<OneBotConfig>() ?? new());
#pragma warning restore IL3050
#pragma warning restore IL2026

        builder.Services.AddSingleton<DstQueryService>();
        builder.Services.AddHostedService<OneBotHostedService>();

        var app = builder.Build();
        app.Run();
    }
}


public class OneBotHostedService(DstQueryService dst, OneBotConfig oneBotConfig, DstConfig dstConfig, ILogger<OneBotHostedService>? logger) : IHostedService
{
    private bool isStopped = false;
    private readonly CqWsSession session = new(new()
    {
        BaseUri = new(oneBotConfig.WebsocketAddress),
        UseApiEndPoint = true,
        UseEventEndPoint = true,
        AccessToken = oneBotConfig.AccessToken
    });


    public Task StartAsync(CancellationToken cancellationToken)
    {
        AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
        {
            logger?.LogError(e.ExceptionObject as Exception, "未经处理的异常");
        };

        //查询服务器
        session.UseGroupMessage(async context =>
        {
            logger?.LogInformation("接收消息: {Message}", context.RawMessage);
            
            // 群白名单
            if (oneBotConfig.IsGroupWhitelist && !oneBotConfig.GroupWhiteList.AsSpan().Contains(context.GroupId))
                return;
            // 群黑名单
            if(!oneBotConfig.IsGroupWhitelist && oneBotConfig.GroupBlackList.AsSpan().Contains(context.GroupId))
                return;
            // 用户白名单
            if (oneBotConfig.IsUserWhitelist && !oneBotConfig.UserWhiteList.AsSpan().Contains(context.UserId))
                return;
            // 用户黑名单
            if(!oneBotConfig.IsUserWhitelist && oneBotConfig.UserBlackList.AsSpan().Contains(context.UserId))
                return;

            string? r;
            try
            {
                CancellationTokenSource cts = new();
                cts.CancelAfter(8000);
                var result = await dst.HandleAsync($"{context.UserId}:{context.GroupId}", context.RawMessage, cts.Token);
                r = result?.Result;
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "查询异常");
                await session.SendGroupMessageAsync(context.GroupId, new CqMessage("获取失败"));
                return;
            }
            if (r != null)
            {
                try
                {
                    await session.SendGroupMessageAsync(context.GroupId, new CqMessage(r));
                }
                catch (Exception ex)
                {
                    logger?.LogError(ex, "发送消息异常");
                }
            }
        });
        
        //获取饥荒版本
        if (!string.IsNullOrWhiteSpace(dstConfig.GetVersionPrompt))
        {
            session.UseGroupMessageMatch(dstConfig.GetVersionPrompt, async context =>
            {
                try
                {
                    CancellationTokenSource cts = new();
                    cts.CancelAfter(6000);
                    if (await dst.GetVersionAsync(cts.Token) is long version and > 0)
                    {
                        await session.SendGroupMessageAsync(context.GroupId, new CqMessage($"饥荒最新版本是 {version}"));
                    }
                    else
                    {
                        await session.SendGroupMessageAsync(context.GroupId, new CqMessage("获取饥荒最新版本失败"));
                    }
                }
                catch (Exception)
                {
                    await session.SendGroupMessageAsync(context.GroupId, new CqMessage("获取饥荒最新版本失败"));
                }
            });
        }

        _ = Task.Run(async () =>
        {
            while (true)
            {
                try
                {
                    await session.StartAsync();
                    logger?.LogInformation("启动成功");

                    await session.WaitForShutdownAsync();
                    await session.StopAsync();

                    if (isStopped)
                        return;

                    logger?.LogWarning("断开 重连...");
                }
                catch (Exception ex)
                {
                    if (!cancellationToken.IsCancellationRequested)
                        logger?.LogError(ex, "异常");

                    if (session.IsConnected)
                    {
                        await session.StopAsync();
                    }
                }
                await Task.Delay(1000, cancellationToken);
            }
        }, cancellationToken);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        isStopped = true;
        return session.StopAsync();
    }
}