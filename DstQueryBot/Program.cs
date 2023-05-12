using EleCho.GoCqHttpSdk;
using EleCho.GoCqHttpSdk.Message;
using EleCho.GoCqHttpSdk.MessageMatching;
using Ilyfairy.DstQueryBot.ServerQuery;

namespace Ilyfairy.DstQueryBot;

internal class Program
{
    static async Task Main(string[] args)
    {
        var config = AppConfig.GetOrCreate();

        CqWsSession session = new(new()
        {
            BaseUri = new(config.Ws),
            UseApiEndPoint = true,
            UseEventEndPoint = true,
        });

        ServerQueryManager dst = new(config.DstQueryConfig);

        //查询服务器
        session.UseGroupMessage(async context =>
        {
            var r = await dst.Input($"{context.UserId}:{context.GroupId}", context.RawMessage);
            if (r != null)
            {
                await session.SendGroupMessageAsync(context.GroupId, new CqMessage(r));
                return;
            }
        });

        //获取饥荒版本
        session.UseGroupMessageMatch(@"^\s*(饥荒版本|获取饥荒版本)\s*$", async context =>
        {
            if (await dst.GetVersionAsync() is long version && version > 0)
            {
                await session.SendGroupMessageAsync(context.GroupId, new CqMessage($"饥荒最新版本是 {version}"));
            }
            else
            {
                await session.SendGroupMessageAsync(context.GroupId, new CqMessage("获取饥荒最新版本失败"));
            }
        });

        //帮助
        session.UseGroupMessageMatch(config.HelpRegex, async context =>
        {
            const string fileName = "dst.png";
            if (File.Exists(fileName))
            {
                var base64 = Convert.ToBase64String(File.ReadAllBytes(fileName));
                await session.SendGroupMessageAsync(context.GroupId, new CqMessage(new CqImageMsg($"base64://{base64}")));
            }
        });

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
                await Console.Out.WriteLineAsync($"异常 重连...{e}");
            }
            await Task.Delay(1000);
        }
    }
}