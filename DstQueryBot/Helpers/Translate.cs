using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ilyfairy.DstQueryBot.Helpers;

public static class Translate
{
    public static string ToChinese(string text)
    {
        return text switch
        {
            "survival" => "生存",
            "relaxed" => "轻松",
            "endless" => "无尽",
            "wilderness" => "荒野",
            "lightsout" => "暗无天日",
            "cooperative" => "合作",
            "lavaarena" => "熔炉",
            "social" => "社交",
            "oceanfishing" or "OceanFishing" => "海钓",

            "wendy" => "温蒂",
            "wilson" => "威尔逊",
            "wathgrithr" => "薇格弗德",
            "wolfgang" => "沃尔夫冈",
            "woodie" => "伍迪",
            "wickerbottom" => "薇克巴顿",
            "waxwell" => "麦斯威尔",
            "wormwood" => "沃姆伍德",
            "wx78" => "WX78",
            "wanda" => "旺达",
            "webber" => "韦伯",
            "wortox" => "沃拓克斯",
            "willow" => "薇洛",
            "warly" => "沃利",
            "wurt" => "沃特",
            "winona" => "薇诺娜",
            "walter" => "沃尔特",
            "wes" => "韦斯",


            _ => text,
        };
    }

    public static string ToEnglish(string text)
    {
        return text switch
        {
            "生存" => "survival",
            "轻松" => "relaxed",
            "无尽" => "endless",
            "荒野" => "wilderness",
            "暗无天日" => "lightsout",
            "合作" => "cooperative",
            "熔炉" => "lavaarena",
            "社交" => "social",
            "海钓" => "oceanfishing",

            "温蒂" => "wendy",
            "威尔逊" => "wilson",
            "薇格弗德" => "wathgrithr",
            "沃尔夫冈" => "wolfgang",
            "伍迪" => "woodie",
            "薇克巴顿" => "wickerbottom",
            "麦斯威尔" => "waxwell",
            "沃姆伍德" => "wormwood",
            "WX78" => "wx78",
            "旺达" => "wanda",
            "韦伯" => "webber",
            "沃拓克斯" => "wortox",
            "薇洛" => "willow",
            "沃利" => "warly",
            "沃特" => "wurt",
            "薇诺娜" => "winona",
            "沃尔特" => "walter",
            "韦斯" => "wes",

            _ => text,
        };
    }
}
