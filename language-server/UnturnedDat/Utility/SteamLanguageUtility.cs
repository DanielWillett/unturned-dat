using System;

namespace UnturnedDat.Data.Utility;

internal static class SteamLanguageUtility
{
    public static string GetInternedLanguageName(ReadOnlySpan<char> fn)
    {
        if (fn.IsEmpty)
            return string.Empty;

        switch (fn[0])
        {
            case 'A':
                if (fn.Equals("Arabic", StringComparison.Ordinal))
                    return "Arabic";
                break;

            case 'B':
                if (fn.Equals("Bulgarian", StringComparison.Ordinal))
                    return "Bulgarian";
                if (fn.Equals("Brazilian", StringComparison.Ordinal))
                    return "Brazilian";
                break;

            case 'C':
                if (fn.Equals("Czech", StringComparison.Ordinal))
                    return "Czech";
                break;

            case 'D':
                if (fn.Equals("Danish", StringComparison.Ordinal))
                    return "Danish";
                if (fn.Equals("Dutch", StringComparison.Ordinal))
                    return "Dutch";
                break;

            case 'E':
                if (fn.Equals("English", StringComparison.Ordinal))
                    return "English";
                break;

            case 'F':
                if (fn.Equals("Finnish", StringComparison.Ordinal))
                    return "Finnish";
                if (fn.Equals("French", StringComparison.Ordinal))
                    return "French";
                break;

            case 'G':
                if (fn.Equals("German", StringComparison.Ordinal))
                    return "German";
                if (fn.Equals("Greek", StringComparison.Ordinal))
                    return "Greek";
                break;

            case 'H':
                if (fn.Equals("Hungarian", StringComparison.Ordinal))
                    return "Hungarian";
                break;

            case 'I':
                if (fn.Equals("Indonesian", StringComparison.Ordinal))
                    return "Indonesian";
                if (fn.Equals("Italian", StringComparison.Ordinal))
                    return "Italian";
                break;

            case 'J':
                if (fn.Equals("Japanese", StringComparison.Ordinal))
                    return "Japanese";
                break;

            case 'K':
                if (fn.Equals("Koreana", StringComparison.Ordinal))
                    return "Koreana";
                break;

            case 'N':
                if (fn.Equals("Norwegian", StringComparison.Ordinal))
                    return "Norwegian";
                break;

            case 'P':
                if (fn.Equals("Polish", StringComparison.Ordinal))
                    return "Polish";
                if (fn.Equals("Portuguese", StringComparison.Ordinal))
                    return "Portuguese";
                break;

            case 'R':
                if (fn.Equals("Romanian", StringComparison.Ordinal))
                    return "Romanian";
                if (fn.Equals("Russian", StringComparison.Ordinal))
                    return "Russian";
                break;

            case 'L':
                if (fn.Equals("Latam", StringComparison.Ordinal))
                    return "Latam";
                break;

            case 'S':
                if (fn.Equals("Schinese", StringComparison.Ordinal))
                    return "Schinese";
                if (fn.Equals("Spanish", StringComparison.Ordinal))
                    return "Spanish";
                if (fn.Equals("Swedish", StringComparison.Ordinal))
                    return "Swedish";
                break;

            case 'T':
                if (fn.Equals("Tchinese", StringComparison.Ordinal))
                    return "Tchinese";
                if (fn.Equals("Thai", StringComparison.Ordinal))
                    return "Thai";
                if (fn.Equals("Turkish", StringComparison.Ordinal))
                    return "Turkish";
                break;

            case 'U':
                if (fn.Equals("Ukrainian", StringComparison.Ordinal))
                    return "Ukrainian";
                break;

            case 'V':
                if (fn.Equals("Vietnamese", StringComparison.Ordinal))
                    return "Vietnamese";
                break;
        }

        return fn.ToString();
    }
}