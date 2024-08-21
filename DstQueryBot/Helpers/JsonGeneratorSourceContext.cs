using System.Text.Json.Serialization;
using DstQueryBot.LobbyModels;

namespace DstQueryBot.Helpers;

[JsonSourceGenerationOptions]
[JsonSerializable(typeof(LobbyResult))]
[JsonSerializable(typeof(DetailsResponse))]
[JsonSerializable(typeof(ListQueryParams))]
internal partial class JsonGeneratorSourceContext : JsonSerializerContext;
