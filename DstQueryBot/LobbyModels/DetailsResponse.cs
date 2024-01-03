namespace Ilyfairy.DstQueryBot.LobbyModels;

public class DetailsResponse
{
    public LobbyDetailsData? Server { get; set; }

    public DateTimeOffset LastUpdate { get; set; }

    public int Code { get; set; }
}
