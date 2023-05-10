using EleCho.GoCqHttpSdk;
using EleCho.GoCqHttpSdk.Message;
using Ilyfairy.DstQueryBot.ServerQuery;

namespace Ilyfairy.DstQueryBot;

internal class Program
{
    static async Task Main(string[] args)
    {
        CqWsSession session = new(new()
        {
            BaseUri = new("ws://127.0.0.1:6700"),
            UseApiEndPoint = true,
            UseEventEndPoint = true,
        });

        ServerQueryManager dst = new();

        session.UseGroupMessage(async context =>
        {
            var r = await dst.Input($"{context.UserId}:{context.GroupId}", context.RawMessage);
            if (r != null)
            {
                await session.SendGroupMessageAsync(context.GroupId, new CqMessage(r));
                return;
            }
            if (new[] { "饥荒版本", "获取饥荒版本" }.Any(v => context.RawMessage.Trim() == v))
            {
                if (await dst.GetVersionAsync() is long version && version > 0)
                {
                    await session.SendGroupMessageAsync(context.GroupId, new CqMessage($"饥荒最新版本是 {version}"));
                }
                else
                {
                    await session.SendGroupMessageAsync(context.GroupId, new CqMessage("获取饥荒最新版本失败"));
                }
            }
        });

        await session.StartAsync();
        Console.WriteLine("启动成功");

        await session.WaitForShutdownAsync();
        Console.WriteLine("结束");
    }
}