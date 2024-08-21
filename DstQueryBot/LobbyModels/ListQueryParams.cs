namespace DstQueryBot.LobbyModels;

/// <summary>
/// 查询参数
/// </summary>
public record class ListQueryParams
{
    /// <summary>
    /// 每页数量
    /// </summary>
    public int? PageCount { get; set; } = 9;
    /// <summary>
    /// 页索引
    /// </summary>
    public int? PageIndex { get; set; }

    /// <summary>
    /// 是否忽略大小写
    /// </summary>
    public bool? IgnoreCase { get; set; } = true;

    /// <summary>
    /// 是否获取详细信息
    /// </summary>
    public bool? IsDetailed { get; set; } = false;


    /// <summary>
    /// 排序, 默认根据字符串HashCode升序排序<br/>
    /// 可以使用|分割,进行多个排序, 使用+-前缀代表升序或者降序排序<br/>
    /// IsExclude属性无效
    /// </summary>
    public ServerStringArray? Sort { get; set; }

    /// <summary>
    /// 服务器名
    /// </summary>
    public ServerRegexValue? ServerName { get; set; }

    /// <summary>
    /// 玩家名
    /// </summary>
    public ServerRegexValue? PlayerName { get; set; }

    /// <summary>
    /// 季节, 可以使用|获取多个季节
    /// </summary>
    public ServerStringArray? Season { get; set; }

    /// <summary>
    /// 服务器版本 可以使用运算符&lt; &lt;= > >= =
    /// </summary>
    public string? Version { get; set; }

    /// <summary>
    /// 游戏模式, 可以使用|获取多个模式
    /// </summary>
    public ServerStringArray? GameMode { get; set; }


    /// <summary>
    /// 游戏风格, 可以使用|获取多个风格
    /// </summary>
    public ServerStringArray? Intent { get; set; }

    /// <summary>
    /// IP地址, 可以使用CIDR或者通配符\*.\*.\*.\*
    /// </summary>
    public string? IP { get; set; }

    /// <summary>
    /// 端口, 可以运算符&lt; &lt;= > >= =
    /// </summary>
    public string? Port { get; set; }

    /// <summary>
    /// 房主的KleiId
    /// </summary>
    public string? Host { get; set; }

    /// <summary>
    /// 平台, 可以使用|来获取多个平台
    /// </summary>
    public ServerStringArray? Platform { get; set; }

    /// <summary>
    /// 国家, 根据IsoCode
    /// </summary>
    public ServerStringArray? Country { get; set; }

    /// <summary>
    /// 根据Mod名搜索
    /// </summary>
    public ServerRegexValue? ModsName { get; set; }

    /// <summary>
    /// 通过ModId搜索Mod, 可以使用|来获取多个Id
    /// </summary>
    public ServerStringArray? ModsId { get; set; }

    /// <summary>
    /// 根据天数信息查询, 可以使用运算符&lt; &lt;= > >= =
    /// </summary>
    public string? Days { get; set; }

    /// <summary>
    /// 季节已过去的天数, 可以运算符&lt; &lt;= > >= =或使用%后缀来表达已过去的百分比
    /// </summary>
    public string? DaysInSeason { get; set; }

    /// <summary>
    /// 描述
    /// </summary>
    public ServerRegexValue? Description { get; set; }

    /// <summary>
    /// 是否有密码
    /// </summary>
    public bool? IsPassword { get; set; }

    /// <summary>
    /// 是否是官方服务器
    /// </summary>
    public bool? IsOfficial { get; set; }

    /// <summary>
    /// 标签, 使用|获取多个标签
    /// </summary>
    public ServerStringArray? Tags { get; set; }

    /// <summary>
    /// 是否PVP
    /// </summary>
    public bool? IsPvp { get; set; }

    /// <summary>
    /// 连接个数, 可以使用%, 或者&lt; &lt;= > >= =
    /// </summary>
    public string? Connected { get; set; }

    /// <summary>
    /// 最大连接个数
    /// </summary>
    public string? MaxConnections { get; set; }

    /// <summary>
    /// 玩家角色
    /// </summary>
    public ServerRegexValue? PlayerPrefab { get; set; }

    /// <summary>
    /// 是否启用Mods
    /// </summary>
    public bool? IsMods { get; set; }

    /// <summary>
    /// 是否是专用服务器
    /// </summary>
    public bool? IsDedicated { get; set; }

    /// <summary>
    /// 所有者的玩家ID
    /// </summary>
    public ServerStringArray? OwnerNetId { get; set; }

    /// <summary>
    /// 是否允许新玩家加入
    /// </summary>
    public bool? IsAllowNewPlayers { get; set; }

    /// <summary>
    /// 服务器是否已暂停
    /// </summary>
    public bool? IsServerPaused { get; set; }

    /// <summary>
    /// 服务器网络类型
    /// </summary>
    public int? Nat { get; set; }

    /// <summary>
    /// 仅限好友加入
    /// </summary>
    public bool? IsFriendsOnly { get; set; }
}

/// <summary>
/// 正则值
/// </summary>
public readonly record struct ServerRegexValue
{
    /// <summary>
    /// 值
    /// </summary>
    public string? Value { get; init; }
    /// <summary>
    /// 是否使用正则
    /// </summary>
    public bool IsRegex { get; init; }
    /// <summary>
    /// 是否排除
    /// </summary>
    public bool IsExclude { get; init; }

    public static implicit operator ServerRegexValue(string? value) => new() { Value = value };
}

/// <summary>
/// 字符串或数组
/// </summary>
public readonly record struct ServerStringArray
{
    /// <summary>
    /// 值
    /// </summary>
    public string?[]? Value { get; init; }

    /// <summary>
    /// 是否排除
    /// </summary>
    public bool IsExclude { get; init; }

    public static implicit operator ServerStringArray(string? value) => new() { Value = [value] };
}
