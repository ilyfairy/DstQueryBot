using Ilyfairy.DstQueryBot.LobbyModels;

namespace Ilyfairy.DstQueryBot.ServerQuery;

public class QueryUser
{
    public SemaphoreSlim Lock { get; } = new(1, 1);
    public LobbyResult? LobbyData { get; set; }
    public QueryType QueryType { get; set; }
    public QueryKeys? Query { get; set; }
}
