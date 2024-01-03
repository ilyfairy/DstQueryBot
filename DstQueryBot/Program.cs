using EleCho.GoCqHttpSdk;
using EleCho.GoCqHttpSdk.Message;
using EleCho.GoCqHttpSdk.MessageMatching;
using Ilyfairy.DstQueryBot.Bot;
using Ilyfairy.DstQueryBot.ServerQuery;
using Serilog;

namespace Ilyfairy.DstQueryBot;

internal class Program
{
    private static AppConfig config = null!;

    static async Task Main()
    {
        config = AppConfig.GetOrCreate();
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Console()
            .CreateLogger();

        await GensokyoMain(); // 连接gensokyo
        //await GoCqHttpMain(); // 连接gocqhttp
    }


    static async Task GensokyoMain()
    {
        GensokyoBot bot = new(config.Ws, config.Http);
        ServerQueryManager dst = new(config.DstQueryConfig);

        AppDomain.CurrentDomain.UnhandledException += (e, sender) =>
        {
            Console.WriteLine($"未经处理的异常:{sender.ExceptionObject}");
        };

        bot.OnMessage += async (sender, e) =>
        {
            if (string.IsNullOrWhiteSpace(e.RawMessage))
                return;

            if (e.MessageType == "group")
            {
                await Console.Out.WriteLineAsync($"接收群消息: {e.RawMessage}");
            }

            var message = e.RawMessage.Trim().TrimStart('/');

            string? r;
            try
            {
                CancellationTokenSource cts = new();
                cts.CancelAfter(5000);
                r = await dst.InputAsync($"{e.GroupId}:{e.UserId}", message, cts.Token);
            }
            catch (Exception ex)
            {
                await bot.SendGroupMessageAsync(e.GroupId, "获取失败");
                Log.Error("异常: {Exception}", ex);
                return;
            }
            if (r != null)
            {
                await bot.SendGroupMessageAsync(e.GroupId, r);
                Console.WriteLine("结束");
                Console.WriteLine();
                return;
            }

            Console.WriteLine("结束");
            Console.WriteLine();
        };


        await Task.Run(async () =>
        {
            while (true)
            {
                try
                {
                    await bot.RunAsync();

                    await Console.Out.WriteLineAsync("断开 重连...");
                }
                catch (Exception e)
                {
                    Console.WriteLine($"异常, 重连...{e}");
                }
                await Task.Delay(1000);
            }
        });

    }


    static async Task GoCqHttpMain()
    {
        CqWsSession session = new(new()
        {
            BaseUri = new(config.Ws),
            UseApiEndPoint = true,
            UseEventEndPoint = true,
            AccessToken = config.AccessToken
        });

        ServerQueryManager dst = new(config.DstQueryConfig);

        AppDomain.CurrentDomain.UnhandledException += (e, sender) =>
        {
            Console.WriteLine($"未经处理的异常:{sender.ExceptionObject}");
        };

        

        async ValueTask<bool> IsNotSend(long groupId)
        {
            var info = await session.GetGroupMemberListAsync(groupId);
            return info?.Members.Any(v => config?.NotSendQQ.Contains(v.UserId) ?? true) ?? true;
        }


        session.UseAny(async (context,next) =>
        {
            _ = next();
        });

        //查询服务器
        session.UseGroupMessage(async context =>
        {
            if (await IsNotSend(context.GroupId)) return;

            await Console.Out.WriteLineAsync($"接收消息: {context.RawMessage}");

            string? r;
            try
            {
                CancellationTokenSource cts = new();
                cts.CancelAfter(5000);
                r = await dst.InputAsync($"{context.UserId}:{context.GroupId}", context.RawMessage, cts.Token);
            }
            catch (Exception ex)
            {
                await session.SendGroupMessageAsync(context.GroupId, new CqMessage("获取失败"));
                Log.Error("异常: {Exception}", ex);
                return;
            }
            if (r != null)
            {
                await session.SendGroupMessageAsync(context.GroupId, new CqMessage(r));
                Console.WriteLine("结束");
                Console.WriteLine();
                return;
            }

            Console.WriteLine("结束");
            Console.WriteLine();
        });

        //获取饥荒版本
        session.UseGroupMessageMatch(@"^\s*(饥荒版本|获取饥荒版本)\s*$", async context =>
        {
            if (await IsNotSend(context.GroupId)) return;

            try
            {
                CancellationTokenSource cts = new();
                cts.CancelAfter(5000);
                if (await dst.GetVersionAsync(cts.Token) is long version && version > 0)
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

        //帮助
        session.UseGroupMessageMatch(config.HelpRegex, async context =>
        {
            if (await IsNotSend(context.GroupId)) return;

            const string fileName = "dst.png";
            if (File.Exists(fileName))
            {
                var base64 = Convert.ToBase64String(File.ReadAllBytes(fileName));
                await session.SendGroupMessageAsync(context.GroupId, new CqMessage(new CqImageMsg($"base64://{base64}")));
            }
        });

        await Task.Run(async () =>
        {
            while (true)
            {
                try
                {
                    await session.StartAsync();
                    Console.WriteLine("启动成功");

                    await session.WaitForShutdownAsync();
                    await session.StopAsync();

                    await Console.Out.WriteLineAsync("断开 重连...");
                }
                catch (Exception e)
                {
                    if (session.IsConnected)
                    {
                        await session.StopAsync();
                    }
                }
                await Task.Delay(1000);
            }
        });


    }
}
