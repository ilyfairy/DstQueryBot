namespace Ilyfairy.DstQueryBot.ServerQuery;

public class QueryKeys
{
    public string? ServerName { get; set; }
    public string? PlayerName { get; set; }
    public string? Host { get; set; }
    public string? IP { get; set; }
    public string? Day { get; set; }
    public string? Mode { get; set; }
    public bool? IsPvP { get; set; }
    public int? ConnectionCount { get; set; }
    public int PageCount { get; set; } = 9;
    public int Page { get; set; } = 0;
    public string Season { get; internal set; }
}
