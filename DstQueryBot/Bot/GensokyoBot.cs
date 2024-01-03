using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Ilyfairy.DstQueryBot.Bot;

public class GensokyoBot
{
    private readonly HttpClient http = new();
    public Uri WsUrl { get; }
    public Uri HttpUrl { get; }

    public event EventHandler<WsMessageResponse>? OnMessage;

    public GensokyoBot(string wsUrl, string httpUrl)
    {
        WsUrl = new Uri(wsUrl);
        HttpUrl = new Uri(httpUrl);
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        ClientWebSocket ws = new();
        await ws.ConnectAsync(WsUrl, cancellationToken);

        var buffer = new byte[4 * 1024];
        StringBuilder s = new(1024);
        while (true)
        {
            var result = await ws.ReceiveAsync(buffer, cancellationToken);
            if (result.MessageType == WebSocketMessageType.Close)
            {
                await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, null, cancellationToken);
                break;
            }
            else if (result.EndOfMessage is false)
            {
                s.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
            }
            else // result.EndOfMessage is true
            {
                s.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));

                var str = s.ToString();
                s.Clear();

                try
                {
                    var msg = JsonSerializer.Deserialize<WsMessageResponse>(str);
                    if (msg is null) continue;

                    OnMessage?.Invoke(this, msg);
                }
                catch (Exception)
                {
                }
            }
        }
    }

    public async Task<HttpResponse> SendGroupMessageAsync(long groupId, string message, CancellationToken cancellationToken = default)
    {
        var url = new Uri(HttpUrl, $"/send_group_msg?group_id={groupId}&message={Uri.EscapeDataString(message)}");
        var str = await http.GetStringAsync(url, cancellationToken);

        var response = JsonSerializer.Deserialize<HttpResponse>(str) ?? new();
        return response;
    }
}

public class HttpResponse
{
    [JsonPropertyName("status")]
    public string Status { get; set; }
}