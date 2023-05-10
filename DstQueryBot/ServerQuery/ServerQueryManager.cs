using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Ilyfairy.DstQueryBot.LobbyModels;

namespace Ilyfairy.DstQueryBot.ServerQuery;

public partial class ServerQueryManager
{
    private readonly HttpClient http = new();
    private string[] ServerTriggers { get; } = { "查服", "查服务器" };
    private string[] PlayerTriggers { get; } = { "查玩家" };

    public ConcurrentDictionary<string, QueryUser> Users { get; } = new(Environment.ProcessorCount, 100);

    public async Task<string?> Input(string userId, string prompt)
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
                            return await GetBriefsDataString(user.LobbyData.List[index - 1].RowId);
                        case ".":
                            if (index <= 0 || index > user.LobbyData.List.Length)
                            {
                                return "超出范围";
                            }
                            return await GetDetailsDataString(user.LobbyData.List[index - 1].RowId);
                        case "p" or "page":
                            if (index <= 0 || index > user.LobbyData.MaxPage + 1)
                            {
                                return "超出范围";
                            }
                            user.Query.Page = index - 1;
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
            if (MatchKey(filter, "模式", "mode") is string mode)
            {
                user.Query.Mode = mode;
            }
            if (MatchKey(filter, "pvp") != null)
            {
                user.Query.IsPvP = true;
            }
            if (MatchKey(filter, "conn", "connect", "connected", "count", "人数", "连接数") is string connectedString)
            {
                if (int.TryParse(connectedString, out var connected))
                {
                    if (connected >= 0) user.Query.ConnectionCount = connected;
                }
            }
            if (MatchKey(filter, "host") is string host)
            {
                user.Query.Host = host;
            }
            if (MatchKey(filter, "day") is string day)
            {
                user.Query.Day = day;
            }
        }

        static string? MatchKey(string str, params string[] keys)
        {
            foreach (var item in keys)
            {
                if (str.StartsWith($"{item} ", StringComparison.OrdinalIgnoreCase))
                {
                    return str[keys.Length..].Trim();
                }
            }
            return null;
        }
    }


    public async Task UpdateList(QueryUser user)
    {
        if (user.Query == null) return;
        user.LobbyData = await GetListAsync(GetUrlQueryList(user.Query!));
    }

    public async Task<string> GetDetailsDataString(string rowId)
    {
        var details = await GetDetailsAsync(rowId);
        StringBuilder s = new(128);
        s.AppendLine($"{details.Name} ({details.Connected}/{details.MaxConnections}) {(details.Password ? "🔒" : "")}");
        s.AppendLine($"地址: {details.Address}:{details.Port}");
        s.AppendLine($"PVP: {(details.PVP ? "是" : "否")}");
        s.AppendLine($"Host: {details.Host}");
        s.AppendLine($"模式: {details.Intent}/{details.Mode}");
        s.AppendLine($"天数信息: 第{details.DaysInfo?.Day.ToString() ?? "未知"}天 {details.Season}({details.DaysInfo?.DaysElapsedInSeason.ToString() ?? "未知"}/{(details.DaysInfo?.DaysElapsedInSeason + details.DaysInfo?.DaysLeftInSeason)?.ToString() ?? "未知"})");
        if (!string.IsNullOrWhiteSpace(details.Desc))
        {
            s.AppendLine($"描述: {details.Desc.Trim()}");
        }
        if (details.Players?.Count > 0)
        {
            s.AppendLine($"玩家: {details.GetPlayersString()}");
        }
        if (details.ModsInfo != null && details.ModsInfo.Count > 0)
        {
            s.AppendLine($"模组: {string.Join(", ", details.ModsInfo.Select(v => $"{v.Name}"))}");
        }
        return s.ToString().Trim();
    }

    public async Task<string?> GetBriefsDataString(string rowId)
    {
        var details = await GetDetailsAsync(rowId);
        StringBuilder s = new(128);
        s.AppendLine($"{details.Name} ({details.Connected}/{details.MaxConnections}) {(details.Password ? "🔒" : "")}");
        s.AppendLine($"模式: {details.Intent}/{details.Mode}");
        s.AppendLine($"天数信息: 第{details.DaysInfo?.Day.ToString() ?? "未知"}天 {details.Season}({details.DaysInfo?.DaysElapsedInSeason.ToString() ?? "未知"}/{(details.DaysInfo?.DaysElapsedInSeason + details.DaysInfo?.DaysLeftInSeason)?.ToString() ?? "未知"})");
        if (!string.IsNullOrWhiteSpace(details.Desc))
        {
            s.AppendLine($"描述: {details.Desc.Trim()}");
        }
        if (details.Players?.Count > 0)
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
        s.AppendLine($"当前是第{user.LobbyData.Page + 1}页 一共{user.LobbyData.MaxPage + 1}页");
        for (int i = 0; i < Math.Min(user.LobbyData.List.Length, user.Query.PageCount); i++)
        {
            s.AppendLine($"{i + 1}. {user.LobbyData.List[i].Name} ({user.LobbyData.List[i].Connected}/{user.LobbyData.List[i].MaxConnections})");
        }
        return s.ToString().Trim();
    }
    public string? GetPlayerQueryString(QueryUser user)
    {
        if (user.LobbyData == null || user.Query == null || user.QueryType != QueryType.Player || user.Query.PlayerName == null) return null;
        if (user.LobbyData.Count == 0) return "没有搜索到相关玩家";
        StringBuilder s = new(128);
        s.AppendLine($"当前是第{user.LobbyData.Page + 1}页 一共{user.LobbyData.MaxPage + 1}页");
        for (int i = 0; i < Math.Min(user.LobbyData.List.Length, user.Query.PageCount); i++)
        {
            s.AppendLine($"{i + 1}. {user.LobbyData.List[i].Name} ({user.LobbyData.List[i].Connected}/{user.LobbyData.List[i].MaxConnections})");
            s.AppendLine($"  玩家: {user.LobbyData.List[i].GetPlayersString(v => v.Name.Contains(user.Query.PlayerName))}");
        }
        return s.ToString().Trim();
    }


    public KeyValuePair<string, string>[] GetUrlQueryList(QueryKeys query)
    {
        List<KeyValuePair<string, string>> queryList = new(8);
        if (query.ConnectionCount is { } conn)
        {
            queryList.Add(new("ConnectedCount", conn.ToString()));
        }
        if (query.Day is { } day)
        {
            queryList.Add(new("Day", day));
        }
        if (query.Host is { } host)
        {
            queryList.Add(new("HostKleiId", host));
        }
        if (query.IP is { } ip)
        {
            queryList.Add(new("IP", ip));
        }
        if (query.ServerName is { } serverName)
        {
            queryList.Add(new("Name", serverName));
        }
        if (query.PlayerName is { } playerName)
        {
            queryList.Add(new("PlayerName", playerName));
        }
        queryList.Add(new("Page", query.Page.ToString()));
        queryList.Add(new("PageCount", query.PageCount.ToString()));
        return queryList.ToArray();
    }


    public void RemoveUser(string userId)
    {
        Users.TryRemove(userId, out _);
    }

    public QueryUser GetOrAddUser(string userId)
    {
        return Users.GetOrAdd(userId, k => new QueryUser());
    }

    public async Task<LobbyDetailsData> GetDetailsAsync(string rowId)
    {
        var response = await http.PostAsync($"https://api.dstserverlist.top/api/details?id={WebUtility.UrlEncode(rowId)}", null);
        var data = await response.Content.ReadFromJsonAsync<LobbyDetailsData>();
        if (data == null) throw new Exception("get dst list failed");
        return data;
    }

    public async Task<LobbyResult> GetListAsync(KeyValuePair<string, string>[]? searchParams = null, CancellationToken cancellationToken = default)
    {
        string? searchParamsString = searchParams == null ? null : string.Join("&", searchParams.Select(v => $"{WebUtility.UrlEncode(v.Key)}={WebUtility.UrlEncode(v.Value)}"));
        var response = await http.PostAsync($"https://api.dstserverlist.top/api/list?{searchParamsString}", null, cancellationToken);
        var data = await response.Content.ReadFromJsonAsync<LobbyResult>();
        if (data == null) throw new Exception("get dst list failed");
        return data;
    }

    public async Task<long?> GetVersionAsync()
    {
        try
        {
            var response = await http.PostAsync($"https://api.dstserverlist.top/api/server/version", null);
            var data = await response.Content.ReadAsStringAsync();
            if (data == null) throw new Exception("get dst list failed");
            JsonNode? json = JsonNode.Parse(data);
            if (json == null) return null;
            return json["version"]?.GetValue<long>();
        }
        catch (Exception)
        {
            return null;
        }
    }

    [GeneratedRegex(@"^(?<type>(\.|p|page))?(?<val>\d+)$")]
    private static partial Regex DstSelectRegex();
}
