namespace DstQueryBot.LobbyModels;

/// <summary>
/// 大厅Mod信息
/// </summary>
public class LobbyModInfo
{
    public long Id { get; set; }
    public required string Name { get; set; }
    public required string CurrentVersion { get; set; }
    public required string NewVersion { get; set; }
    public bool IsClientDownload { get; set; }
}
