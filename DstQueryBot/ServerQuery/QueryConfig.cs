using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ilyfairy.DstQueryBot.ServerQuery;

public class QueryConfig
{
    /// <summary>
    /// 是否显示平台信息
    /// </summary>
    public bool IsShowPlatformType { get; set; } = false;

    /// <summary>
    /// IP是否替换成逗号
    /// </summary>
    public bool IsIPReplace { get; set; }
}
