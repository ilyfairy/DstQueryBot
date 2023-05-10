using System.Text.Json.Serialization;

namespace Ilyfairy.DstQueryBot.LobbyModels;

/// <summary>
/// 单个服务器列表详细信息
/// </summary>
public class LobbyDetailsData
{
    public string Name { get; set; } //房间名称

    public string Address { get; set; } //ip地址

    public int Port { get; set; } //端口

    public string RowId { get; set; } //房间id

    public int Connected { get; set; } //在线玩家个数

    public bool Dedicated { get; set; } //是否是专用服务器

    public string Host { get; set; } //房主KleiID

    //[JsonConverter(typeof(EnumConverter<IntentionType>))]
    public string Intent { get; set; } //风格

    public int MaxConnections { get; set; } //最大玩家限制

    //[JsonConverter(typeof(EnumConverter<GameMode>))]
    public string Mode { get; set; } //模式

    public bool Mods { get; set; } //是否开启mod

    public bool Password { get; set; } //是否需要密码

    //[JsonConverter(typeof(EnumConverter<Platform>))]
    public string Platform { get; set; } //平台信息

    //[JsonConverter(typeof(EnumConverter<Season>))]
    public string Season { get; set; } //季节

    [JsonPropertyName("pvp")]
    public bool PVP { get; set; } //是否启用pvp

    [JsonPropertyName("v")]
    public int Version { get; set; } //版本

    [JsonPropertyName("session")]
    public string Session { get; set; } //会话id

    public string? Country { get; set; }

    public List<LobbyPlayerInfo> Players { get; set; } //玩家信息

    public long LastPing { get; set; } //上次与大厅通信时间

    public string SteamClanId { get; set; } //steam群组gid

    public object Slaves { get; set; } //json

    public object Secondaries { get; set; } //json

    public bool ClanOnly { get; set; } //仅限steam群组成员加入

    public bool Fo { get; set; } //是否仅限好友加入

    public string Guid { get; set; } //GUID

    public bool ClientHosted { get; set; } //是否是客户端主机

    public string OwnerNetId { get; set; } //steamid

    public string[] Tags { get; set; } //Tags

    public bool LanOnly { get; set; } //是否仅局域网

    public string Desc { get; set; } //房间描述

    public int Tick { get; set; } //Tick

    public bool ClientModsOff { get; set; }

    public int Nat { get; set; } //服务器网络类型  公网5内网7

    public bool AllowNewPlayers { get; set; } //是否允许新玩家加入

    public bool Event { get; set; }

    public bool ValveCloudServer { get; set; }

    public string ValvePopId { get; set; }

    public string ValveRoutingInfo { get; set; }

    public bool KleiOfficial { get; set; } //是否是官方服务器

    public bool ServerPaused { get; set; } //世界是否暂停

    public LobbyDayInfo DaysInfo { get; set; } //天数信息

    //TODO: 未完成
    public object WorldGen { get; set; } //世界配置

    public string SteamId { get; set; }

    public string SteamRoom { get; set; }

    public object Users { get; set; } //始终为null

    public List<LobbyModInfo> ModsInfo { get; set; } //mod信息


    public string? GetPlayersString(Func<LobbyPlayerInfo, bool>? func = null)
    {
        if (Players is null || Players.Count == 0) return null;
        var players = string.Join(", ", Players.Where(func ?? (v => true)).Select(v =>
        {
            string prefab = v.Prefab;
            if (string.IsNullOrWhiteSpace(prefab)) prefab = "未选择";
            return $"{v.Name}({prefab})";
        }));
        return players;
    }
}
