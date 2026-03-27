using System.Text.RegularExpressions;

namespace DART.BlackduckAnalysis
{
    public static class RecommendedFixFormatter
    {
        public static string Format(string? input)
        {
            var text = input ?? string.Empty;

            // Basic cleanup: remove bullets/brackets/newlines and parenthetical notes
            text = Regex.Replace(text, "[\\*\\[\\]\\n]", string.Empty).Trim();
            text = Regex.Replace(text, "\\([^)]+\\)", string.Empty).Trim();

            // Drop any trailing guidance about latest stable releases
            text = Regex.Replace(text, @"The latest stable releases.*$", string.Empty, RegexOptions.IgnoreCase).Trim();

            // Remove commit references like "by this commit" (with or without trailing period)
            text = Regex.Replace(text, @"\s*by this commit\.?", string.Empty, RegexOptions.IgnoreCase).Trim();

            // Normalize whitespace
            text = Regex.Replace(text, @"\s{2,}", " ").Trim();

            // Extract semantic versions
            var matches = Regex.Matches(text, @"\b\d+\.\d+\.\d+(?:[-+][\w\.-]+)?\b");
            var versions = matches.Select(m => m.Value).ToList();

            if (versions.Count == 0)
            {
                return text;
            }

            // Preserve a consistent prefix: always use "Fixed in " when phrase is present
            var hasAnyFixedIn = Regex.IsMatch(input ?? string.Empty, @"Fixed in", RegexOptions.IgnoreCase);
            var prefix = hasAnyFixedIn ? "Fixed in " : string.Empty;
            var result = prefix + string.Join(", ", versions);

            return result;
        }
    }
}
