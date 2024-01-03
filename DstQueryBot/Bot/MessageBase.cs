using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Ilyfairy.DstQueryBot.Bot;

public class MessageBase
{
    [JsonPropertyName("type")]
    public string Type { get; set; }

    [JsonPropertyName("data")]
    public JsonObject Data { get; set; }
}