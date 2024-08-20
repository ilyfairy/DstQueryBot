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
    /// 是否启用白名单, 否则启用黑名单
    /// </summary>
    public bool IsWhitelist { get; set; } = false;
    public long[] WhiteList { get; set; } = [];
    public long[] BlackList { get; set; } = [];
}
