namespace Ilyfairy.DstQueryBot.LobbyModels;

public class LobbyResult
{
    public DateTime DateTime { get; set; }
    public DateTime LastUpdate { get; set; }
    public int Count { get; set; }
    public int AllCount { get; set; }
    public int MaxPage { get; set; }
    public int Page { get; set; }
    public LobbyDetailsData[] List { get; set; }
}
