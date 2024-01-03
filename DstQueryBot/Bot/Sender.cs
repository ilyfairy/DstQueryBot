using System.Text.Json.Serialization;

namespace Ilyfairy.DstQueryBot.Bot;

public class Sender
{
    [JsonPropertyName("area")]
    public string Area { get; set; }

    [JsonPropertyName("level")]
    public string Level { get; set; }

    [JsonPropertyName("nickname")]
    public string Nickname { get; set; }

    [JsonPropertyName("role")]
    public string Role { get; set; }

    [JsonPropertyName("sex")]
    public string Sex { get; set; }

    [JsonPropertyName("tiny_id")]
    public string TinyId { get; set; }

    [JsonPropertyName("user_id")]
    public long UserId { get; set; }
}
