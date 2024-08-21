namespace DstQueryBot.LobbyModels;

public class LobbyResult
{
    public DateTimeOffset DateTime { get; set; }
    public DateTimeOffset LastUpdate { get; set; }
    public int Count { get; set; }
    public int TotalCount { get; set; }
    public int MaxPageIndex { get; set; }
    public int PageIndex { get; set; }
    public required LobbyDetailsData[] List { get; set; }
}
