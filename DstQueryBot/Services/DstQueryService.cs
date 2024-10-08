﻿using System.Collections.Concurrent;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using DstQueryBot.Helpers;
using DstQueryBot.LobbyModels;
using DstQueryBot.Models;
using Microsoft.Extensions.Logging;
using SmartFormat;

namespace DstQueryBot.Services;

public class DstQueryService
{
    private readonly ILogger<DstQueryService>? _logger;
    private static readonly char[] _newLineSplitChars = ['\r', '\n'];

    private readonly SmartFormatter _smartFormatter;

    private readonly HttpClient _http = new();

    public ConcurrentDictionary<string, DstContext> Users { get; } = new(4, 17);

    public DstConfig DstConfig { get; }

    private readonly Regex? _searchServersPromptRegexCache;
    private readonly Regex? _searchPlayersPromptRegexCache;

    public DstQueryService(DstConfig dstConfig, ILogger<DstQueryService>? logger)
    {
        DstConfig = dstConfig;
        _logger = logger;
        _smartFormatter = Smart.CreateDefaultSmartFormat(new SmartFormat.Core.Settings.SmartSettings()
        {
            CaseSensitivity = SmartFormat.Core.Settings.CaseSensitivityType.CaseInsensitive,
            Formatter =
            {
                ErrorAction = SmartFormat.Core.Settings.FormatErrorAction.Ignore,
            }
        });
        Smart.Default = _smartFormatter;

        if (!string.IsNullOrWhiteSpace(DstConfig.SearchServersPrompt))
            _searchServersPromptRegexCache = new Regex(DstConfig.SearchServersPrompt, RegexOptions.IgnoreCase | RegexOptions.Compiled);
        if (!string.IsNullOrWhiteSpace(DstConfig.SearchPlayerPrompt))
            _searchPlayersPromptRegexCache = new Regex(DstConfig.SearchPlayerPrompt, RegexOptions.IgnoreCase | RegexOptions.Compiled);
    }


    /// <summary>
    /// 处理查询请求
    /// </summary>
    /// <param name="id">用户ID</param>
    /// <param name="input">输入内容</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>查询结果</returns>
    public async Task<DstQueryResult?> HandleAsync(string id, string input, CancellationToken cancellationToken = default)
    {
        var context = Users.GetOrAdd(id, _ => new DstContext() { LastTriggerTime = DateTimeOffset.Now });

        try
        {
            await context.Lock.WaitAsync(cancellationToken);

            var firstLine = input;
            if (input.IndexOfAny(_newLineSplitChars) is int lineIndex and not -1)
            {
                firstLine = firstLine[..lineIndex];
            }

            bool isOutputList = false; // 是否查询列表
            bool isShowBrief = false; // 是否显示简要信息
            bool isShowDetailed = false; // 是否显示详细信息
            int selectedNumber = 1; // 选择的序号
            const string SplitString = ", "; // 分隔符

            // 超时, 重新查询
            if (DateTimeOffset.Now - context.LastTriggerTime > TimeSpan.FromSeconds(DstConfig.Timeout))
            {
                Users[id] = context with { Lock = context.Lock };
            }
            context.LastTriggerTime = DateTimeOffset.Now;

            // 查询服务器
            if (_searchServersPromptRegexCache?.Match(firstLine) is { Success: true } serverMatch)
            {
                var text = serverMatch.Groups["Text"].Value;
                _logger?.LogInformation("查询服务器: {Text}", text);

                ListQueryParams listQueryParams = new();
                listQueryParams.ServerName = text;
                context.QueryParams = listQueryParams;
                context.IsShowTargetPlayersInList = false;
                isOutputList = true;
            }
            // 查询玩家
            else if (_searchPlayersPromptRegexCache?.Match(firstLine) is { Success: true } playerMatch)
            {
                var text = playerMatch.Groups["Text"].Value;
                _logger?.LogInformation("查询玩家: {Text}", text);

                ListQueryParams listQueryParams = new();
                listQueryParams.PlayerName = text;
                context.QueryParams = listQueryParams;
                context.IsShowTargetPlayersInList = true;
                isOutputList = true;
            }
            // 上一页
            else if (!string.IsNullOrWhiteSpace(DstConfig.PreviousPagePrompt)
                && Regex.IsMatch(firstLine, DstConfig.PreviousPagePrompt))
            {
                if (context.QueryParams != null)
                {
                    context.PageIndex = Math.Min(0, context.PageIndex - 1);
                    isOutputList = true;
                }
            }
            // 下一页
            else if (!string.IsNullOrWhiteSpace(DstConfig.NextPagePrompt)
                && Regex.IsMatch(firstLine, DstConfig.PreviousPagePrompt))
            {
                if (context.QueryParams != null && context.ListResponse != null)
                {
                    context.PageIndex = Math.Min(context.ListResponse.MaxPageIndex, context.PageIndex + 1);
                    isOutputList = true;
                }
            }
            // 显示简要信息
            else if (!string.IsNullOrWhiteSpace(DstConfig.ShowBriefInfoPrompt)
                && Regex.Match(firstLine, DstConfig.ShowBriefInfoPrompt) is { Success: true } showBriefMatch
                && context.ListResponse is not null)
            {
                if (!int.TryParse(showBriefMatch.Groups["Number"].Value, out var number))
                {
                    if (DstConfig.IsRemovedWhenInputInvalid)
                    {
                        Users.TryRemove(id, out _);
                    }
                    return null; // 解析失败 可能数字太长
                }
                if (number <= 0 || number > context.ListResponse.Count)
                {
                    number = number <= 0 ? 1 : context.ListResponse.Count;
                }
                selectedNumber = number;
                isShowBrief = true;
            }
            // 显示详细信息
            else if (!string.IsNullOrWhiteSpace(DstConfig.ShowDetailedInfoPrompt)
                && Regex.Match(firstLine, DstConfig.ShowDetailedInfoPrompt) is { Success: true } showDetailedMatch
                && context.ListResponse is not null)
            {
                if (!int.TryParse(showDetailedMatch.Groups["Number"].Value, out var number))
                {
                    if (DstConfig.IsRemovedWhenInputInvalid)
                    {
                        Users.TryRemove(id, out _);
                    }
                    return null; // 解析失败 可能数字太长
                }
                if (number <= 0 || number > context.ListResponse.Count)
                {
                    number = number <= 0 ? 1 : context.ListResponse.Count;
                }
                selectedNumber = number;
                isShowDetailed = true;
            }
            // 切换页
            else if (!string.IsNullOrWhiteSpace(DstConfig.SwitchPagePrompt)
                && Regex.Match(firstLine, DstConfig.SwitchPagePrompt) is { Success: true } switchPageMatch
                && context.ListResponse is not null)
            {
                if (!int.TryParse(switchPageMatch.Groups["Page"].Value, out var page))
                {
                    if (DstConfig.IsRemovedWhenInputInvalid)
                    {
                        Users.TryRemove(id, out _);
                    }
                    return null; // 解析失败 可能数字太长
                }
                page--;
                if (page <= 0 || page > context.ListResponse.MaxPageIndex)
                {
                    page = page <= 0 ? 1 : context.ListResponse.MaxPageIndex;
                }
                context.PageIndex = page;
                isOutputList = true;
            }
            else
            {
                if (DstConfig.IsRemovedWhenInputInvalid || context.QueryParams is null)
                {
                    Users.TryRemove(id, out _);
                }
                return null;
            }

            // 列表过滤
            if (context.QueryParams is { } queryParams && firstLine != input)
            {
                HandleFilter();

                void HandleFilter()
                {
                    bool isFirst = true;
                    foreach (var line in input.AsSpan().EnumerateLines())
                    {
                        if (isFirst && !(isFirst = false)) // 跳过首行
                            continue;

                        var trim = line.Trim();
                        if (trim.Length == 0)
                            continue;

                        var lineString = line.ToString();
                        // Host, 房主KleinId
                        if (Regex.Match(lineString, @"^Host(:?\s*|\s+)(?<Host>[a-z_\d]{3,})$", RegexOptions.IgnoreCase) is { Success: true } hostMatch)
                        {
                            var host = hostMatch.Groups["Host"].Value;
                            queryParams.Host = host;
                        }
                        // IP
                        else if (Regex.Match(lineString, @"^IP(:?\s*|\s+)(?<IP>[<>=\*\d\.]{3,})$", RegexOptions.IgnoreCase) is { Success: true } ipMatch)
                        {
                            var ip = ipMatch.Groups["IP"].Value;
                            queryParams.IP = ip;
                        }
                        // 天数 & 季节已过去天数百分比
                        else if (Regex.Match(lineString, @"^(days?|天数?)(:?\s*|\s+)(?<Days>([<>=]{1,2})?\d+%?)$", RegexOptions.IgnoreCase) is { Success: true } daysMatch)
                        {
                            var daysExpression = daysMatch.Groups["Days"].Value;
                            queryParams.Days = daysExpression;
                        }
                        // 是否PvP
                        else if (Regex.Match(lineString, @"^(PvP(:?\s*|\s+)(?<PvP>.*)|(?<PvP>PvP))$", RegexOptions.IgnoreCase) is { Success: true } pvpMatch)
                        {
                            var pvp = pvpMatch.Groups["PvP"].Value;
                            queryParams.IsPvp = pvp switch
                            {
                                "是" => true,
                                "否" => false,
                                _ when string.Equals(pvp, "pvp", StringComparison.OrdinalIgnoreCase) => true,
                                _ when string.Equals(pvp, "true", StringComparison.OrdinalIgnoreCase) => true,
                                _ when string.Equals(pvp, "false", StringComparison.OrdinalIgnoreCase) => false,
                                _ when string.Equals(pvp, "yes", StringComparison.OrdinalIgnoreCase) => true,
                                _ when string.Equals(pvp, "no", StringComparison.OrdinalIgnoreCase) => false,
                                _ => null,
                            };
                        }
                        // 端口
                        else if (Regex.Match(lineString, @"^Port(:?\s*|\s+)(?<Port>([<>=]{1,2})?\d+)$", RegexOptions.IgnoreCase) is { Success: true } portMatch)
                        {
                            var port = portMatch.Groups["Port"].Value;
                            queryParams.Port = port;
                        }
                        // 游戏平台
                        else if (Regex.Match(lineString, @"^((平台|Platform)(:?\s*|\s+)(?<Platform>.*)|(?<Platform>(Steam|WeGame|PlayStation|Xbox|Switch|QQGame|PS4Official|\|)+))$", RegexOptions.IgnoreCase) is { Success: true } platformMatch)
                        {
                            var platform = platformMatch.Groups["Platform"].Value;
                            queryParams.Platform = platform;
                        }
                        // 是否需要密码
                        else if (Regex.Match(lineString, @"^(((Is)?Password|Lock|密码)(:?\s*|\s+)(?<IsPassword>.*)|(?<IsPassword>(Lock|Unlock|Password|NoPassword|Passwd)))$", RegexOptions.IgnoreCase) is { Success: true } passwordMatch)
                        {
                            var isPassword = passwordMatch.Groups["IsPassword"].Value;
                            queryParams.IsPassword = isPassword switch
                            {
                                "是" => true,
                                "否" => false,
                                _ when string.Equals(isPassword, "true", StringComparison.OrdinalIgnoreCase) => true,
                                _ when string.Equals(isPassword, "false", StringComparison.OrdinalIgnoreCase) => false,
                                _ when string.Equals(isPassword, "yes", StringComparison.OrdinalIgnoreCase) => true,
                                _ when string.Equals(isPassword, "no", StringComparison.OrdinalIgnoreCase) => false,
                                _ when string.Equals(isPassword, "Lock", StringComparison.OrdinalIgnoreCase) => true,
                                _ when string.Equals(isPassword, "Unlock", StringComparison.OrdinalIgnoreCase) => false,
                                _ when string.Equals(isPassword, "Password", StringComparison.OrdinalIgnoreCase) => true,
                                _ when string.Equals(isPassword, "NoPassword", StringComparison.OrdinalIgnoreCase) => false,
                                _ when string.Equals(isPassword, "Passwd", StringComparison.OrdinalIgnoreCase) => true,
                                _ => null,
                            };
                        }
                        // 季节
                        else if (Regex.Match(lineString, @"^(Season|季节)(:?\s*|\s+)(?<Season>.*)$", RegexOptions.IgnoreCase) is { Success: true } seasonMatch)
                        {
                            var season = seasonMatch.Groups["Season"].Value;
                            queryParams.Season = Translate.ToEnglish(season);
                        }
                        // 正则匹配
                        else if (Regex.Match(lineString, @"^(正则(表达式)?|Regex|rx)$", RegexOptions.IgnoreCase) is { Success: true })
                        {
                            if (context.QueryParams.ServerName is { IsRegex: false, Value: { } }) // not null and not regex
                            {
                                context.QueryParams.ServerName = context.QueryParams.ServerName.Value with { IsRegex = true };
                            }
                            else if (context.QueryParams.PlayerName is { IsRegex: false, Value: { } }) // not null and not regex
                            {
                                context.QueryParams.PlayerName = context.QueryParams.PlayerName.Value with { IsRegex = true };
                            }
                        }
                        // 精确匹配
                        else if (Regex.Match(lineString, @"^(精确(匹配)?|Exact)$", RegexOptions.IgnoreCase) is { Success: true })
                        {
                            if (context.QueryParams.ServerName is { IsRegex: false, Value: { } }) // not null and not regex
                            {
                                context.QueryParams.ServerName = new()
                                {
                                    Value = $"^{Regex.Escape(context.QueryParams.ServerName.Value.Value)}$",
                                    IsRegex = true,
                                };
                            }
                            else if (context.QueryParams.PlayerName is { IsRegex: false, Value: { } }) // not null and not regex
                            {
                                context.QueryParams.PlayerName = new()
                                {
                                    Value = $"^{Regex.Escape(context.QueryParams.PlayerName.Value.Value)}$",
                                    IsRegex = true,
                                };
                            }
                        }

                    }
                }
            }

            // 获取服务器信息
            if ((isShowBrief || isShowDetailed) && context.ListResponse is not null)
            {
                StringBuilder s = new();

                var server = context.ListResponse.List[selectedNumber - 1];
                LobbyDetailsData detailedServer;
                try
                {
                    detailedServer = await GetDetailsAsync(server.RowId, cancellationToken);
                }
                catch (Exception)
                {
                    try
                    {
                        // 重试一次
                        detailedServer = await GetDetailsAsync(server.RowId, cancellationToken);
                    }
                    catch (Exception)
                    {
                        detailedServer = server;
                    }
                }

                var param = new DetailedServerFormatParams
                {
                    Name = detailedServer.Name,
                    CurrentPlayerCount = detailedServer.Connected,
                    MaxPlayerCount = detailedServer.MaxConnections,
                    IsPassword = detailedServer.IsPassword,
                    Mode = Translate.ToChinese(detailedServer.Mode),
                    Intent = Translate.ToChinese(detailedServer.Intent),
                    Days = detailedServer.DaysInfo?.Day,
                    Season = Translate.ToChinese(detailedServer.Season),
                    DaysElapsedInSeason = detailedServer.DaysInfo?.DaysElapsedInSeason,
                    DaysLeftInSeason = detailedServer.DaysInfo?.DaysLeftInSeason,
                    TotalDaysSeason = detailedServer.DaysInfo?.TotalDaysSeason,
                    IP = detailedServer.Address.IP,
                    Port = detailedServer.Port,
                    IsPvP = detailedServer.IsPvp,
                    Host = detailedServer.Host,
                    Description = detailedServer.Description
                };

                // 名称
                s.AppendLineSmart("{Name} ({CurrentPlayerCount}/{MaxPlayerCount})", param);

                // 地址
                if (isShowDetailed)
                    s.AppendLineSmart("地址: {IP}:{Port}", param);

                // 是否PvP
                if (isShowDetailed)
                    s.AppendLineSmart("PvP: {IsPvP:是|否}", param); // {条件:True文本|False文本}

                // Host
                if (isShowDetailed)
                    s.AppendLineSmart("Host: {Host}", param); // 房主KleiID

                // 模式
                s.AppendLineSmart("模式: {Mode}/{Intent}", param);

                // 天数
                s.AppendLineSmart("天数信息: 第{Days:isnull:未知|{Days}}天 {Season}({DaysElapsedInSeason:isnull:未知|{DaysElapsedInSeason}}/{TotalDaysSeason:isnull:未知|{TotalDaysSeason}})", param);

                // 描述
                if (isShowDetailed && !string.IsNullOrWhiteSpace(detailedServer.Description))
                    s.AppendLineSmart("描述: {Description}", param);

                // 玩家
                if (detailedServer.Players is { Length: > 0 })
                {
                    int playerIndex = 0;
                    //s.Append("玩家: ");
                    s.AppendLine("-------- 玩家列表 --------");
                    foreach (var player in detailedServer.Players ?? [])
                    {
                        var playerItemParams = new PlayerItemFormatParams()
                        {
                            Number = ++playerIndex,
                            PlayerName = player.Name,
                            Id = player.NetId,
                            Color = player.Color,
                            PlayerPrefab = player.Prefab,
                            TranslatedPlayerPrefab = Translate.ToChinese(player.Prefab switch
                            {
                                "" => "未选择",
                                _ => player.Prefab,
                            }),
                        };
                        s.AppendLineSmart("{Number}. {PlayerName}({TranslatedPlayerPrefab})", playerItemParams);
                        //s.Append(SplitString);
                    }
                    //s.TrimEnd(SplitString);
                }

                // 模组
                if (isShowDetailed && detailedServer.ModsInfo is { Length: > 0 })
                {
                    int modIndex = 0;
                    //s.Append("模组: ");
                    s.AppendLine("-------- 模组列表 --------");
                    foreach (var mod in detailedServer.ModsInfo ?? [])
                    {
                        var modItemParams = new ModItemFormatParams()
                        {
                            Number = ++modIndex,
                            ModName = mod.Name,
                            ModVersion = mod.CurrentVersion, // 当前版本
                            ModNewVersion = mod.NewVersion, // 最新版本
                            IsClientDownload = mod.IsClientDownload, // 是否需要客户端下载
                        };
                        s.AppendLineSmart("{Number}. {ModName}", modItemParams);
                        //s.Append(SplitString);
                    }
                    //s.TrimEnd(SplitString);
                }

                s.TrimEndNewLine();
                return new DstQueryResult(context, s.ToString());
            }
            // 查询服务器列表
            else if (isOutputList && context.QueryParams is not null)
            {
                context.QueryParams.PageIndex = context.PageIndex;
                context.QueryParams.PageCount = DstConfig.PageMaxSize;
                var list = await GetListAsync(context.QueryParams!, cancellationToken);
                context.ListResponse = list;

                if (list.Count == 0)
                {
                    if (context.QueryParams.PlayerName is not null)
                    {
                        return new DstQueryResult(context, DstConfig.NotFoundPlayerText);
                    }
                    else
                    {
                        return new DstQueryResult(context, DstConfig.NotFoundServerText);
                    }
                }
                else
                {
                    StringBuilder s = new();
                    var pageSize = Math.Min(list.Count, DstConfig.PageMaxSize);

                    var pageParams = new PageFormatParams
                    {
                        PageNumber = list.PageIndex + 1,
                        MaxPageNumber = list.MaxPageIndex + 1,
                    };
                    s.AppendLineSmart("当前是第{PageNumber}页 一共{MaxPageNumber}页", pageParams);
                    for (int i = 0; i < pageSize; i++)
                    {
                        var server = list.List[i];
                        var listItemParams = new ListItemFormatParams
                        {
                            ItemNumber = i + 1,
                            ServerName = server.Name,
                            CurrentPlayerCount = server.Connected,
                            MaxPlayerCount = server.MaxConnections,
                            IsPassword = server.IsPassword,
                            Platform = server.Platform,
                            Host = server.Host,
                        };

                        var serverString = _smartFormatter.Format(DstConfig.ListItemFormat, listItemParams);
                        s.AppendLine(serverString);
                        if (context.IsShowTargetPlayersInList)
                        {
                            var targetPlayers = server.Players?.Where(v =>
                            {
                                if (context.QueryParams?.PlayerName?.Value is null) return true;
                                if (context.QueryParams.PlayerName.Value.IsRegex)
                                {
                                    return Regex.IsMatch(v.Name, context.QueryParams.PlayerName.Value.Value);
                                }
                                else
                                {
                                    return v.Name.Contains(context.QueryParams.PlayerName.Value.Value);
                                }
                            }) ?? [];
                            int playerIndex = 0;
                            s.Append("  玩家: ");
                            foreach (var player in targetPlayers)
                            {
                                var playerParam = new PlayerItemFormatParams
                                {
                                    Number = ++playerIndex,
                                    PlayerName = player.Name,
                                    Id = player.NetId,
                                    Color = player.Color,
                                    PlayerPrefab = player.Prefab,
                                    TranslatedPlayerPrefab = Translate.ToChinese(player.Prefab switch
                                    {
                                        "" => "未选择",
                                        _ => player.Prefab,
                                    }),
                                };
                                s.AppendSmart("{PlayerName}({PlayerPrefab})", playerParam);
                                s.Append(SplitString);
                            }
                            s.TrimEnd(SplitString);
                            s.AppendLine();
                        }
                    }
                    s.TrimEndNewLine();
                    return new DstQueryResult(context, s.ToString());
                }
            }

            return null;
        }
        finally
        {
            context.Lock.Release();
        }
    }



    #region Api

    public async Task<LobbyDetailsData> GetDetailsAsync(string rowId, CancellationToken cancellationToken = default)
    {
        var url = $"https://api.dstserverlist.top/api/v2/server/details/{Uri.EscapeDataString(rowId)}";
        _logger?.LogInformation("获取详细信息  RowId:{RowId}  URL:{Url}", rowId, url);
        var response = await _http.PostAsync(url, null, cancellationToken);
        var data = await response.Content.ReadFromJsonAsync<DetailsResponse>(JsonGeneratorSourceContext.Default.DetailsResponse, cancellationToken);
        if (data == null || data.Code is not 200) throw new Exception("get dst list failed");
        return data.Server!;
    }

    public async Task<LobbyResult> GetListAsync(ListQueryParams searchParams, CancellationToken cancellationToken = default)
    {
        var url = $"https://api.dstserverlist.top/api/v2/server/list";
        _logger?.LogInformation("请求列表  URL:{Url}", url);

        var response = await _http.PostAsync(url, JsonContent.Create(searchParams, JsonGeneratorSourceContext.Default.ListQueryParams), cancellationToken);
        var data = await response.Content.ReadFromJsonAsync(JsonGeneratorSourceContext.Default.LobbyResult, cancellationToken);
        if (data == null) throw new Exception("获取服务器列表失败");
        return data;
    }

    public async Task<long?> GetVersionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var version = await _http.GetStringAsync($"https://api.dstserverlist.top/api/v2/server/version", cancellationToken);
            if (version == null) throw new Exception("获取版本失败");
            return long.Parse(version);
        }
        catch (Exception)
        {
            return null;
        }
    }

    #endregion
}

public record DstQueryResult(DstContext Context, string? Result);

file record DetailedServerFormatParams
{
    public required string Name { get; set; }
    public required int CurrentPlayerCount { get; set; }
    public required int MaxPlayerCount { get; set; }
    public required bool IsPassword { get; set; }
    public required string Mode { get; set; }
    public required string Intent { get; set; }
    public required int? Days { get; set; }
    public required string? Season { get; set; }
    public required int? DaysElapsedInSeason { get; set; }
    public required int? DaysLeftInSeason { get; set; }
    public required int? TotalDaysSeason { get; set; }
    public required string IP { get; set; }
    public required int Port { get; set; }
    public required bool IsPvP { get; set; }
    public required string? Host { get; set; }
    public required string? Description { get; set; }
}

file record PlayerItemFormatParams
{
    public required int Number { get; set; }
    public required string PlayerName { get; set; }
    public required string TranslatedPlayerPrefab { get; set; }
    public required string PlayerPrefab { get; set; }
    public required string Color { get; set; }
    public required string Id { get; set; }
}

file record ModItemFormatParams
{
    public required int Number { get; set; }
    public required string ModName { get; set; }
    public required string ModVersion { get; set; }
    public required string ModNewVersion { get; set; }
    public required bool IsClientDownload { get; set; }
}

file record ListItemFormatParams
{
    public required int ItemNumber { get; set; }
    public required string ServerName { get; set; }
    public required int CurrentPlayerCount { get; set; }
    public required int MaxPlayerCount { get; set; }
    public required bool IsPassword { get; set; }
    public required string Platform { get; set; }
    public required string? Host { get; set; }
}

file record PageFormatParams
{
    public required int PageNumber { get; set; }
    public required int MaxPageNumber { get; set; }

}