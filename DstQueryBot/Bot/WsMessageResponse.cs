using Ilyfairy.DstQueryBot.Helpers;
using System.Text.Json.Serialization;

namespace Ilyfairy.DstQueryBot.Bot;

public class WsMessageResponse
{
    [JsonPropertyName("avatar")]
    public string Avatar { get; set; }

    [JsonPropertyName("font")]
    public long Font { get; set; }

    [JsonPropertyName("group_id")]
    public long GroupId { get; set; }

    [JsonPropertyName("message")]
    public MessageBase[] Messages { get; set; }

    [JsonPropertyName("message_id")]
    public long MessageId { get; set; }

    [JsonPropertyName("message_seq")]
    public long MessageSeq { get; set; }

    [JsonPropertyName("message_type")]
    public string MessageType { get; set; }

    [JsonPropertyName("post_type")]
    public string PostType { get; set; }

    [JsonPropertyName("raw_message")]
    public string RawMessage { get; set; }

    [JsonPropertyName("real_message_type")]
    public string RealMessageType { get; set; }

    [JsonPropertyName("self_id")]
    public long SelfId { get; set; }

    [JsonPropertyName("sender")]
    public Sender Sender { get; set; }

    [JsonPropertyName("sub_type")]
    public string SubType { get; set; }

    [JsonPropertyName("time")]
    [JsonConverter(typeof(DateTimeOffsetSecConverter))]
    public DateTimeOffset Time { get; set; }

    [JsonPropertyName("user_id")]
    public long UserId { get; set; }
}
