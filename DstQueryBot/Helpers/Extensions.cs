using System.Text;

namespace DstQueryBot.Helpers;

internal static class Extensions
{
    public static StringBuilder TrimEndNewLine(this StringBuilder stringBuilder)
    {
        while (stringBuilder.Length > 0 && stringBuilder[^1] is '\r' or '\n')
        {
            stringBuilder.Remove(stringBuilder.Length - 1, 1);
        }
        return stringBuilder;
    }

    public static StringBuilder TrimEnd(this StringBuilder stringBuilder, string str)
    {
        if (str.Length == 0) return stringBuilder;
        
        while (true)
        {
            for (int i = 0; i < str.Length; i++)
            {
                if (stringBuilder.Length < str.Length || stringBuilder[^(i + 1)] != str[^(i + 1)])
                {
                    return stringBuilder;
                }
            }
            stringBuilder.Remove(stringBuilder.Length - str.Length, str.Length);
        }
    }

}
