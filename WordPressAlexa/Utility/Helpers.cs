using System;
using System.Text.RegularExpressions;
using System.Web;

namespace WordPressAlexa.Utility
{
    public static class Helpers
    {
        public static string ScrubHtml(string value)
        {
            var step = Regex.Replace(value, @"<[^>]+>|&nbsp;", "").Trim();
            step = ScrubCustom(step);
            step = Regex.Replace(step, @"http[^\s]+", "").Trim(); // remove urls
            step = Regex.Replace(step, @"\p{Cs}", "").Trim(); // remove UTF-16 surrogate code units, e.g. Emoticons
            step = HttpUtility.HtmlDecode(step);
            step = step.Replace("&", "und");
            step = Regex.Replace(step, @"\s{2,}", " "); // remove additional whitespace
            return step;
        }

        public static string ScrubCustom(string value)
        {
            var step = value;

            // remove sources form post end
            var indexOfText = value.IndexOf("Quelle: ", StringComparison.InvariantCultureIgnoreCase);
            if (indexOfText >= 0)
                step = step.Remove(indexOfText);

            return step;
        }
    }
}
