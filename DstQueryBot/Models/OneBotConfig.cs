using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DstQueryBot.Models;

public class OneBotConfig
{
    public string WebsocketAddress { get; set; } = "ws://127.0.0.1:7000";
    public string? AccessToken { get; set; }

    /// <summary>
    /// 是否开启群白名单, 否则启用黑名单
    /// </summary>
    public bool IsGroupWhitelist { get; set; } = false;
    public long[] GroupWhiteList { get; set; } = [];
    public long[] GroupBlackList { get; set; } = [];

    /// <summary>
    /// 是否开启用户白名单, 否则启用黑名单
    /// </summary>
    public bool IsUserWhitelist { get; set; } = false;
    public long[] UserWhiteList { get; set; } = [];
    public long[] UserBlackList { get; set; } = [];
}
