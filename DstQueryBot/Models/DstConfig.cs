using System.Diagnostics.CodeAnalysis;

namespace DstQueryBot.Models;

public class DstConfig
{
    /// <summary>
    /// 每页最大大小
    /// </summary>
    public int PageMaxSize { get; set; } = 9;

    /// <summary>
    /// 超时时间(秒), 超时后需要重新查询
    /// </summary>
    public double Timeout { get; set; } = 10 * 60;

    /// <summary>
    /// 是否在输入无效时删除上下文
    /// </summary>
    public bool IsRemovedWhenInputInvalid { get; set; } = true;


    //`(*>﹏<*)′=================================== 一条华丽的分割线 ===================================`(*>﹏<*)′//


    /// <summary>
    /// 查询饥荒版本
    /// </summary>
    [StringSyntax(StringSyntaxAttribute.Regex)]
    public string? GetVersionPrompt { get; set; } = @"^(获取|查询)饥荒版本$";

    /// <summary>
    /// 查询服务器, 仅匹配第一行, 其他行用来输入过滤<br/>
    /// Text捕获组为服务器名称<br/>
    /// 如果为空则没有此功能
    /// </summary>
    [StringSyntax(StringSyntaxAttribute.Regex)]
    public string? SearchServersPrompt { get; set; } = @"^查服\s*(?<Text>.*)$";

    /// <summary>
    /// 查询玩家, 仅匹配第一行, 其他行用来输入过滤<br/>
    /// Text捕获组为玩家名<br/>
    /// 如果为空则没有此功能
    /// </summary>
    [StringSyntax(StringSyntaxAttribute.Regex)]
    public string? SearchPlayerPrompt { get; set; } = @"^查玩家\s*(?<Text>.*)$";

    /// <summary>
    /// 上一页<br/>
    /// 如果为空则没有此功能
    /// </summary>
    [StringSyntax(StringSyntaxAttribute.Regex)]
    public string PreviousPagePrompt { get; set; } = @"^\s*上一页\s*$";

    /// <summary>
    /// 下一页<br/>
    /// 如果为空则没有此功能
    /// </summary>
    [StringSyntax(StringSyntaxAttribute.Regex)]
    public string? NextPagePrompt { get; set; } = @"^\s*下一页\s*$";

    /// <summary>
    /// 显示简要信息<br/>
    /// Number捕获组是列表的编号(从1开始)<br/>
    /// 如果为空则没有此功能
    /// </summary>
    [StringSyntax(StringSyntaxAttribute.Regex)]
    public string ShowBriefInfoPrompt { get; set; } = @"^(?<Number>\d+)$";

    /// <summary>
    /// 显示详细信息<br/>
    /// Number捕获组是列表的编号(从1开始)<br/>
    /// 如果为空则没有此功能
    /// </summary>
    [StringSyntax(StringSyntaxAttribute.Regex)]
    public string ShowDetailedInfoPrompt { get; set; } = @"^(\.(?<Number>\d+))|((?<Number>\d+)\.)$"; // .1 或 1.

    /// <summary>
    /// 切换到任意一页<br/>
    /// Page捕获组为页码<br/>
    /// 如果为空则没有此功能
    /// </summary>
    [StringSyntax(StringSyntaxAttribute.Regex)]
    public string SwitchPagePrompt { get; set; } = @"^p(?<Page>\d+)$"; // page1
    

    //`(*>﹏<*)′=================================== 一条华丽的分割线 ===================================`(*>﹏<*)′//


    /// <summary>
    /// 没有找到相关服务器时提示<br/>
    /// 如果为空则不提示
    /// </summary>
    public string? NotFoundServerText { get; set; } = "没有搜索到相关服务器";

    /// <summary>
    /// 没有找到相关玩家时提示<br/>
    /// 如果为空则不提示
    /// </summary>
    public string? NotFoundPlayerText { get; set; } = "没有搜索到相关玩家";

    /// <summary>
    /// 列表每一项的格式<br/>
    /// ItemNumber: 页码<br/>
    /// ServerName: 服务器名<br/>
    /// CurrentPlayerCount: 当前玩家数量<br/>
    /// MaxPlayerCount: 最大玩家数量<br/>
    /// Lock: 当需要服务器密码时显示的锁文本<br/>
    /// Platform: 平台<br/>
    /// Host: KU_xxx(房主KleiID)<br/>
    /// </summary>
    [StringSyntax(StringSyntaxAttribute.CompositeFormat)]
    public string ListItemFormat { get; set; } = "{ItemNumber}. {ServerName} ({CurrentPlayerCount}/{MaxPlayerCount})";

}
