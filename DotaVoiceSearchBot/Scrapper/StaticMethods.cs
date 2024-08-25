using System.Text.RegularExpressions;
namespace DotaVoiceSearchBot.Scrapper;

public static class StaticMethods
{
    public static string Sanitize(this string text)
    {
        return Regex.Replace(text, "[^0-9A-Za-z _-]", "");
    }
}