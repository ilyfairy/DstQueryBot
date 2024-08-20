using DstQueryBot.LobbyModels;

namespace DstQueryBot.Models;

public record class DstContext
{
    public DateTimeOffset LastTriggerTime { get; set; }

    /// <summary>
    /// 如果搜索玩家时, 则在列表中显示玩家名
    /// </summary>
    public bool IsShowTargetPlayersInList { get; set; }

    /// <summary>
    /// 如果过滤IP时, 则在列表中显示IP
    /// </summary>
    public bool IsShowIPInList { get; set; }

    /// <summary>
    /// 页索引
    /// </summary>
    public int PageIndex { get; set; }
    /// <summary>
    /// 页码
    /// </summary>
    public int PageNumber => PageNumber + 1;


    /// <summary>
    /// 异步锁, 当用户在查询时, 不能再次查询
    /// </summary>
    public SemaphoreSlim Lock { get; internal set; } = new(1, 1);

    /// <summary>
    /// 服务器列表响应
    /// </summary>
    public LobbyResult? ListResponse { get; set; }

    /// <summary>
    /// 翻页时要用
    /// </summary>
    public ListQueryParams? QueryParams { get; set; }
}
