using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Ilyfairy.DstQueryBot.Helpers;
using Ilyfairy.DstQueryBot.LobbyModels;
using Serilog;

namespace Ilyfairy.DstQueryBot.ServerQuery;

public partial class ServerQueryManager
{
    private readonly HttpClient http = new();
    private string[] ServerTriggers { get; } = { "查服", "查服务器" };
    private string[] PlayerTriggers { get; } = { "查玩家" };

    public ConcurrentDictionary<string, QueryUser> Users { get; } = new(Environment.ProcessorCount, 100);

    public async Task<string?> InputAsync(string userId, string prompt, CancellationToken cancellationToken)
    {
        string[] split = prompt.Split("\n", StringSplitOptions.RemoveEmptyEntries).Select(v => v.Trim()).ToArray();
        if (split.Length == 0)
        {
            RemoveUser(userId);
            return null;
        }

        var user = GetOrAddUser(userId);

        try
        {
            await user.Lock.WaitAsync();
            var first = split[0];

            foreach (var item in ServerTriggers)
            {
                if (first.StartsWith(item))
                {
                    user.Query = new();
                    SetQueryServer(user, first[item.Length..].Trim());
                    SetFilter(user, split.Skip(1));
                    await UpdateList(user);
                    return GetServerQueryString(user);
                }
            }

            foreach (var item in PlayerTriggers)
            {
                if (first.StartsWith(item))
                {
                    user.Query = new();
                    SetQueryPlayer(user, first[item.Length..].Trim());
                    SetFilter(user, split.Skip(1));
                    await UpdateList(user);
                    return GetPlayerQueryString(user);
                }
            }

            if (user.LobbyData != null && user.Query != null)
            {
                var select = DstSelectRegex().Match(first);
                if (select.Success)
                {
                    if (!int.TryParse(select.Groups["val"].Value, out var index))
                    {
                        return "索引错误";
                    }

                    switch (select.Groups["type"].Value)
                    {
                        case "":
                            if (index <= 0 || index > user.LobbyData.List.Length)
                            {
                                return "超出范围";
                            }
                            return await GetBriefsDataStringAsync(user.LobbyData.List[index - 1].RowId, cancellationToken);
                        case ".":
                            if (index <= 0 || index > user.LobbyData.List.Length)
                            {
                                return "超出范围";
                            }
                            return await GetDetailsDataStringAsync(user.LobbyData.List[index - 1].RowId, cancellationToken);
                        case "p" or "page":
                            if (index <= 0 || index > user.LobbyData.MaxPageIndex + 1)
                            {
                                return "超出范围";
                            }
                            user.Query.PageIndex = index - 1;
                            await UpdateList(user);
                            return user.QueryType switch
                            {
                                QueryType.Server => GetServerQueryString(user),
                                QueryType.Player => GetPlayerQueryString(user),
                                _ => null,
                            };
                        default:
                            break;
                    }
                }
            }
            RemoveUser(userId);
            return null;
        }
        catch (Exception)
        {
            throw;
        }
        finally
        {
            user.Lock.Release();
        }
    }

    public QueryConfig Config { get; }

    public ServerQueryManager(QueryConfig? config = null)
    {
        Config = config ?? new();
    }

    #region SetValue
    public void SetQueryServer(QueryUser user, string serverName)
    {
        if (user.Query is null) return;
        user.QueryType = QueryType.Server;
        user.Query.ServerName = serverName;
    }

    public void SetQueryPlayer(QueryUser user, string playerName)
    {
        if (user.Query is null) return;
        user.QueryType = QueryType.Player;
        user.Query.PlayerName = playerName;
    }

    public void SetFilter(QueryUser user, IEnumerable<string> filters)
    {
        if (user.Query is null) return;
        foreach (var filter in filters)
        {
            if (string.IsNullOrWhiteSpace(filter)) continue;
            if (MatchKey(filter, "ip") is string ip)
            {
                user.Query.IP = ip;
            }
            if (MatchKey(filter, "模式", "mode", "gamemode") is string mode)
            {
                user.Query.GameMode = Translate.ToEnglish(mode);
            }
            if (MatchKey(filter, "pvp") != null)
            {
                user.Query.IsPvp = true;
            }
            if (MatchKey(filter, "conn", "connect", "connected", "count", "人数", "连接数") is string connectedString)
            {
                user.Query.Connected = connectedString;
            }
            if (MatchKey(filter, "season", "季节") is string season)
            {
                user.Query.Season = Translate.ToEnglish(season);
            }
            if (MatchKey(filter, "host") is string host)
            {
                user.Query.Host = host;
            }
            if (MatchKey(filter, "day", "天数") is string day)
            {
                user.Query.Days = day;
            }
            if (MatchKey(filter, "预设", "角色", "prefab") is string prefab)
            {
                user.Query.PlayerPrefab = Translate.ToEnglish(prefab);
            }
        }

        static string? MatchKey(string str, params string[] keys)
        {
            foreach (var item in keys)
            {
                if (str.StartsWith($"{item}", StringComparison.OrdinalIgnoreCase))
                {
                    return str[item.Length..].Trim();
                }
            }
            return null;
        }
    }
    #endregion


    #region GetString
    public async Task<string> GetDetailsDataStringAsync(string rowId, CancellationToken cancellationToken)
    {
        var details = await GetDetailsAsync(rowId, cancellationToken);
        StringBuilder s = new(128);
        s.AppendLine($"{details.Name} ({details.Connected}/{details.MaxConnections}) {(details.IsPassword ? "🔒" : "")}");

        string ip = details.Address.IP;
        if (Config.IsIPReplace)
            ip = ip.Replace(".", ",");

        s.AppendLine($"地址: {ip}:{details.Port}");
        s.AppendLine($"PVP: {(details.IsPvp ? "是" : "否")}");
        s.AppendLine($"Host: {details.Host}");
        s.AppendLine($"模式: {Translate.ToChinese(details.Intent)}/{Translate.ToChinese(details.Mode)}");
        s.AppendLine($"天数信息: 第{details.DaysInfo?.Day.ToString() ?? "未知"}天 {details.Season}({details.DaysInfo?.DaysElapsedInSeason.ToString() ?? "未知"}/{(details.DaysInfo?.DaysElapsedInSeason + details.DaysInfo?.DaysLeftInSeason)?.ToString() ?? "未知"})");
        if (!string.IsNullOrWhiteSpace(details.Description))
        {
            s.AppendLine($"描述: {details.Description.Trim()}");
        }
        if (details.Players?.Length > 0)
        {
            s.AppendLine($"玩家: {details.GetPlayersString()}");
        }
        if (details.ModsInfo != null && details.ModsInfo.Length > 0)
        {
            s.AppendLine($"模组: {string.Join(", ", details.ModsInfo.Select(v => $"{v.Name}"))}");
        }
        return s.ToString().Trim();
    }
    public async Task<string?> GetBriefsDataStringAsync(string rowId, CancellationToken cancellationToken)
    {
        var details = await GetDetailsAsync(rowId, cancellationToken);
        StringBuilder s = new(128);
        s.AppendLine($"{details.Name} ({details.Connected}/{details.MaxConnections}) {(details.IsPassword ? "🔒" : "")}");
        s.AppendLine($"模式: {Translate.ToChinese(details.Intent)}/{Translate.ToChinese(details.Mode)}");
        s.AppendLine($"天数信息: 第{details.DaysInfo?.Day.ToString() ?? "未知"}天 {details.Season}({details.DaysInfo?.DaysElapsedInSeason.ToString() ?? "未知"}/{(details.DaysInfo?.DaysElapsedInSeason + details.DaysInfo?.DaysLeftInSeason)?.ToString() ?? "未知"})");
        if (!string.IsNullOrWhiteSpace(details.Description))
        {
            s.AppendLine($"描述: {details.Description.Trim()}");
        }
        if (details.Players?.Length > 0)
        {
            s.AppendLine($"玩家: {details.GetPlayersString()}");
        }
        return s.ToString().Trim();
    }

    public string? GetServerQueryString(QueryUser user)
    {
        if (user.LobbyData == null || user.Query == null || user.QueryType != QueryType.Server) return null;
        if (user.LobbyData.Count == 0) return "没有搜索到任何结果";
        StringBuilder s = new(128);
        s.AppendLine($"当前是第{user.LobbyData.PageIndex + 1}页 一共{user.LobbyData.MaxPageIndex + 1}页");
        for (int i = 0; i < Math.Min(user.LobbyData.List.Length, user.Query.PageCount ?? 0); i++)
        {
            s.AppendLine($"{i + 1}. {GetServerSingleLineString(user.LobbyData.List[i])}");
        }
        return s.ToString().Trim();
    }

    public string? GetPlayerQueryString(QueryUser user)
    {
        if (user.LobbyData == null || user.Query == null || user.QueryType != QueryType.Player || user.Query.PlayerName == null) return null;
        if (user.LobbyData.Count == 0) return "没有搜索到相关玩家";
        StringBuilder s = new(128);
        s.AppendLine($"当前是第{user.LobbyData.PageIndex + 1}页 一共{user.LobbyData.MaxPageIndex + 1}页");
        for (int i = 0; i < Math.Min(user.LobbyData.List.Length, user.Query.PageCount ?? 0); i++)
        {
            s.AppendLine($"{i + 1}. {GetServerSingleLineString(user.LobbyData.List[i])}");
            s.AppendLine($"  玩家: {user.LobbyData.List[i].GetPlayersString(v => v.Name.Contains(user.Query.PlayerName.Value.Value))}");
        }
        return s.ToString().Trim();
    }
    #endregion

    public async Task UpdateList(QueryUser user)
    {
        if (user.Query == null) return;
        user.LobbyData = await GetListAsync(user.Query);
    }

    public void RemoveUser(string userId)
    {
        Users.TryRemove(userId, out _);
    }
    public QueryUser GetOrAddUser(string userId)
    {
        return Users.GetOrAdd(userId, k => new QueryUser());
    }

    #region GetStringExtension
    private string GetServerSingleLineString(LobbyDetailsData data)
    {
        if (Config.IsShowPlatformType)
        {
            return $"{data.Name} [{data.Platform}] ({data.Connected}/{data.MaxConnections})";
        }
        else
        {
            return $"{data.Name} ({data.Connected}/{data.MaxConnections})";
        }
    }
    #endregion

    #region Api
    public async Task<LobbyDetailsData> GetDetailsAsync(string rowId, CancellationToken cancellationToken = default)
    {
        var url = $"https://api.dstserverlist.top/api/v2/server/details/{WebUtility.UrlEncode(rowId)}";
        Log.Information("获取详细信息  RowId:{RowId}  URL:{RUL}", rowId, url);
        var response = await http.PostAsync(url, null, cancellationToken);
        var data = await response.Content.ReadFromJsonAsync<DetailsResponse>(cancellationToken);
        if (data == null || data.Code is not 200) throw new Exception("get dst list failed");
        return data.Server!;
    }

    public async Task<LobbyResult> GetListAsync(ListQueryParams searchParams = null, CancellationToken cancellationToken = default)
    {
        var url = $"https://api.dstserverlist.top/api/v2/server/list";
        Log.Information("请求列表  URL:{RUL}", url);
        var json = JsonSerializer.Serialize(searchParams);

        var response = await http.PostAsync(url, new StringContent(json, null, "application/json"), cancellationToken);
        string str = await response.Content.ReadAsStringAsync(cancellationToken);
        LobbyResult? data;
        try
        {
             data = JsonSerializer.Deserialize<LobbyResult>(str);
        }
        catch (Exception e)
        {
            Log.Error("反序列化异常 Json:{Json}", str);
            throw;
        }
        if (data == null) throw new Exception("get dst list failed");
        return data;
    }

    public async Task<long?> GetVersionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var version = await http.GetStringAsync($"https://api.dstserverlist.top/api/v2/server/version", cancellationToken);
            if (version == null) throw new Exception("get dst list failed");
            return long.Parse(version);
        }
        catch (Exception)
        {
            return null;
        }
    }
    #endregion

    [GeneratedRegex(@"^((?<type>(\.|p|page))?(?<val>\d+)|(?<val>\d+)(?<type>(\.)))$")]
    private static partial Regex DstSelectRegex();
}
