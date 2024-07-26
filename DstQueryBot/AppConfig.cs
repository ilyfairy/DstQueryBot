using Ilyfairy.DstQueryBot.ServerQuery;
using System.Text.Json;

namespace Ilyfairy.DstQueryBot;

public  class AppConfig
{
    public const string FileName = "config.json";
    
    public string AccessToken { get; set; } = "";
    public string Ws { get; set; } = "ws://127.0.0.1:6700";
    public string Http { get; set; } = "ws://127.0.0.1:6800";
    public string HelpRegex { get; set; } = "#help dst";

    public QueryConfig DstQueryConfig { get; set; } = new();

    /// <summary>
    /// 如果群里包含指定QQ, 则不发送消息
    /// </summary>
    public long[] NotSendQQ { get; set; } = Array.Empty<long>();

    public static AppConfig GetOrCreate()
    {
        if (File.Exists(FileName))
        {
            var obj = JsonSerializer.Deserialize<AppConfig>(File.ReadAllText(FileName)) ?? throw new ArgumentNullException();
            return obj;
        }
        else
        {
            AppConfig config = new();
            File.WriteAllText(FileName, JsonSerializer.Serialize(config, new JsonSerializerOptions()
            {
                WriteIndented = true
            }));
            return config;
        }
    }
}
