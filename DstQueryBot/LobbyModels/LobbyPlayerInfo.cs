namespace DstQueryBot.LobbyModels;

/// <summary>
/// 大厅玩家信息
/// </summary>
public class LobbyPlayerInfo
{
    /// <summary>
    /// 玩家名
    /// </summary>
    public required string Name { get; set; }
    /// <summary>
    /// 游戏内颜色
    /// </summary>
    public required string Color { get; set; }
    public required int EventLevel { get; set; }
    public required string NetId { get; set; }
    /// <summary>
    /// 角色
    /// </summary>
    public required string Prefab { get; set; }
}
