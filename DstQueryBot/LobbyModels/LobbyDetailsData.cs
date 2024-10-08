﻿using DstQueryBot.Helpers;
using DstServerQuery.Models;
using System.Text.Json.Serialization;

namespace DstQueryBot.LobbyModels;

/// <summary>
/// 单个服务器列表详细信息
/// </summary>
public class LobbyDetailsData
{
    [JsonPropertyName("Name")]
    public required string Name { get; set; } //房间名称

    [JsonPropertyName("Address")]
    public required IPAddressInfo Address { get; set; } //ip地址信息

    [JsonPropertyName("Port")]
    public int Port { get; set; } //端口

    [JsonPropertyName("RowId")]
    public required string RowId { get; set; } //房间id

    [JsonPropertyName("Connected")]
    public int Connected { get; set; } //在线玩家个数

    [JsonPropertyName("IsDedicated")]
    public bool IsDedicated { get; set; } //是否是专用服务器

    [JsonPropertyName("Host")]
    public string? Host { get; set; } //房主KleiID

    [JsonPropertyName("Intent")]
    public required string Intent { get; set; } //风格

    [JsonPropertyName("MaxConnections")]
    public int MaxConnections { get; set; } //最大玩家限制

    [JsonPropertyName("Mode")]
    public required string Mode { get; set; } //模式

    [JsonPropertyName("IsMods")]
    public bool IsMods { get; set; } //是否开启mod

    [JsonPropertyName("IsPassword")]
    public bool IsPassword { get; set; } //是否需要密码

    [JsonPropertyName("Platform")]
    public required string Platform { get; set; } //平台信息

    [JsonPropertyName("Season")]
    public string? Season { get; set; } //季节

    [JsonPropertyName("IsPvp")]
    public bool IsPvp { get; set; } //是否启用pvp

    [JsonPropertyName("Version")]
    public long Version { get; set; } //版本

    [JsonPropertyName("Session")]
    public string? Session { get; set; } //会话id




    [JsonPropertyName("IsClanOnly")]
    public bool IsClanOnly { get; set; } //仅限steam群组成员加入

    [JsonPropertyName("IsFriendsOnly")]
    public bool IsFriendsOnly { get; set; } //是否仅限好友加入

    [JsonPropertyName("Slaves")]
    public WorldLevelItem[]? Slaves { get; set; } //json

    [JsonPropertyName("Secondaries")]
    public WorldLevelItem[]? Secondaries { get; set; } //json

    [JsonPropertyName("IsAllowNewPlayers")]
    public bool IsAllowNewPlayers { get; set; } //是否允许新玩家加入

    [JsonPropertyName("IsServerPaused")]
    public bool IsServerPaused { get; set; } //世界是否暂停

    [JsonPropertyName("SteamId")]
    public string? SteamId { get; set; }

    [JsonPropertyName("SteamRoom")]
    public string? SteamRoom { get; set; }

    [JsonPropertyName("Tags")]
    public string[]? Tags { get; set; } //Tags

    [JsonPropertyName("Guid")]
    public string? Guid { get; set; } //GUID

    [JsonPropertyName("IsClientHosted")]
    public bool IsClientHosted { get; set; } //是否是客户端主机

    [JsonPropertyName("SteamClanId")]
    public string? SteamClanId { get; set; } //steam群组gid

    [JsonPropertyName("OwnerNetId")]
    public string? OwnerNetId { get; set; } //steamid

    [JsonPropertyName("IsLanOnly")]
    public bool IsLanOnly { get; set; } //是否仅局域网




    [JsonPropertyName("Players")]
    public LobbyPlayerInfo[]? Players { get; set; } //玩家信息

    [JsonPropertyName("LastPing")]
    public long? LastPing { get; set; } //上次与大厅通信时间

    [JsonPropertyName("Description")]
    public string? Description { get; set; } //房间描述

    [JsonPropertyName("Tick")]
    public int? Tick { get; set; } //Tick

    [JsonPropertyName("IsClientModsOff")]
    public bool? IsClientModsOff { get; set; }

    [JsonPropertyName("Nat")]
    public int? Nat { get; set; } //服务器网络类型  公网5内网7

    [JsonPropertyName("IsEvent")]
    public bool? IsEvent { get; set; }

    [JsonPropertyName("IsValveCloudServer")]
    public bool? IsValveCloudServer { get; set; }

    [JsonPropertyName("ValvePopId")]
    public string? ValvePopId { get; set; }

    [JsonPropertyName("ValveRoutingInfo")]
    public string? ValveRoutingInfo { get; set; }

    [JsonPropertyName("IsKleiOfficial")]
    public bool? IsKleiOfficial { get; set; } //是否是官方服务器

    [JsonPropertyName("DaysInfo")]
    public LobbyDaysInfo? DaysInfo { get; set; } //天数信息

    [JsonPropertyName("WorldGen")]
    public object? WorldGen { get; set; } //世界配置

    [JsonPropertyName("Users")]
    public object? Users { get; set; } //始终为null

    [JsonPropertyName("ModsInfo")]
    public LobbyModInfo[]? ModsInfo { get; set; } //mod信息




    public string? GetPlayersString(Func<LobbyPlayerInfo, bool>? func = null)
    {
        if (Players is null || Players.Length == 0) return null;
        var players = string.Join(", ", Players.Where(func ?? (v => true)).Select(v =>
        {
            string prefab = v.Prefab;
            if (string.IsNullOrWhiteSpace(prefab)) prefab = "未选择";
            return $"{v.Name}({Translate.ToChinese(prefab)})";
        }));
        return players;
    }
}
