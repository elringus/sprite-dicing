using System;

namespace UnityCommon
{
    public static class StringUtils
    {
        /// <summary>
        /// Whether compared string is literally-equal (independent of case).
        /// </summary>
        public static bool LEquals (this string content, string comparedString)
        {
            return content.Equals(comparedString, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Performs <see cref="string.Equals(string, string, StringComparison)"/> with <see cref="StringComparison.Ordinal"/>.
        /// </summary>
        public static bool EqualsFast (this string content, string comparedString)
        {
            return content.Equals(comparedString, StringComparison.Ordinal);
        }

        /// <summary>
        /// Performs <see cref="string.EndsWith(string, StringComparison)"/> with <see cref="StringComparison.Ordinal"/>.
        /// </summary>
        public static bool EndsWithFast (this string content, string match)
        {
            return content.EndsWith(match, StringComparison.Ordinal);
        }

        /// <summary>
        /// Performs <see cref="string.StartsWith(string, StringComparison)"/> with <see cref="StringComparison.Ordinal"/>.
        /// </summary>
        public static bool StartsWithFast (this string content, string match)
        {
            return content.StartsWith(match, StringComparison.Ordinal);
        }

        /// <summary>
        /// Performs <see cref="string.StartsWith(string)"/> and <see cref="string.EndsWith(string)"/> with the provided match.
        /// </summary>
        public static bool WrappedIn (this string content, string match, StringComparison comp = StringComparison.Ordinal)
        {
            return content.StartsWith(match, comp) && content.EndsWith(match, comp);
        }

        /// <summary>
        /// Attempts to extract a subset of the provided <paramref name="source"/> string, starting at
        /// <paramref name="startIndex"/> and ending at <paramref name="endIndex"/>; returns <see langword="null"/> on fail.
        /// </summary>
        /// <param name="source">The string to extract the subset from.</param>
        /// <param name="startIndex">Start index of the subset.</param>
        /// <param name="endIndex">End index of the subset.</param>
        /// <returns>The extracted subset string or <see langword="null"/> if failed.</returns>
        public static string TrySubset (string source, int startIndex, int endIndex)
        {
            if (string.IsNullOrWhiteSpace(source)) return null;
            if (startIndex < 0 || startIndex >= source.Length) return null;
            if (endIndex < 0 || endIndex >= source.Length) return null;
            if (endIndex - startIndex < 0) return null;

            var length = endIndex - startIndex + 1;
            return source.Substring(startIndex, length);
        }

        /// <summary>
        /// Attempts to extract content between the specified matches (on first occurence).
        /// </summary>
        public static string GetBetween (this string content, string startMatch, string endMatch, StringComparison comp = StringComparison.Ordinal)
        {
            if (content.Contains(startMatch) && content.Contains(endMatch))
            {
                var startIndex = content.IndexOf(startMatch, comp) + startMatch.Length;
                var endIndex = content.IndexOf(endMatch, startIndex, comp);
                return content.Substring(startIndex, endIndex - startIndex);
            }
            else return null;
        }

        /// <summary>
        /// Attempts to extract content wrapped in the specified match (on first occurence).
        /// </summary>
        public static string GetBetween (this string content, string match, StringComparison comp = StringComparison.Ordinal)
        {
            return content.GetBetween(match, match, comp);
        }

        /// <summary>
        /// Attempts to extract content before the specified match (on first occurence).
        /// </summary>
        public static string GetBefore (this string content, string matchString, StringComparison comp = StringComparison.Ordinal)
        {
            if (content.Contains(matchString))
            {
                var endIndex = content.IndexOf(matchString, comp);
                return content.Substring(0, endIndex);
            }
            else return null;
        }

        /// <summary>
        /// Attempts to extract content before the specified match (on last occurence).
        /// </summary>
        public static string GetBeforeLast (this string content, string matchString, StringComparison comp = StringComparison.Ordinal)
        {
            if (content.Contains(matchString))
            {
                var endIndex = content.LastIndexOf(matchString, comp);
                return content.Substring(0, endIndex);
            }
            else return null;
        }

        /// <summary>
        /// Attempts to extract content after the specified match (on last occurence).
        /// </summary>
        public static string GetAfter (this string content, string matchString, StringComparison comp = StringComparison.Ordinal)
        {
            if (content.Contains(matchString))
            {
                var startIndex = content.LastIndexOf(matchString, comp) + matchString.Length;
                if (content.Length <= startIndex) return string.Empty;
                return content.Substring(startIndex);
            }
            else return null;
        }

        /// <summary>
        /// Attempts to extract content after the specified match (on first occurence).
        /// </summary>
        public static string GetAfterFirst (this string content, string matchString, StringComparison comp = StringComparison.Ordinal)
        {
            if (content.Contains(matchString))
            {
                var startIndex = content.IndexOf(matchString, comp) + matchString.Length;
                if (content.Length <= startIndex) return string.Empty;
                return content.Substring(startIndex);
            }
            else return null;
        }

        /// <summary>
        /// Splits the string using new line symbol as a separator.
        /// Will split by all type of new lines, independant of environment.
        /// </summary>
        public static string[] SplitByNewLine (this string content, StringSplitOptions splitOptions = StringSplitOptions.None)
        {
            if (string.IsNullOrEmpty(content)) return null;

            // "\r\n"   (\u000D\u000A)  Windows
            // "\n"     (\u000A)        Unix
            // "\r"     (\u000D)        Mac
            // Not using Environment.NewLine here, as content could've been produced 
            // in not the same environment we running the program in.
            return content.Split(new string[] { "\r\n", "\n", "\r" }, splitOptions);
        }

        /// <summary>
        /// Removes mathing trailing string.
        /// </summary>
        public static string TrimEnd (this string source, string value)
        {
            if (!source.EndsWithFast(value))
                return source;

            return source.Remove(source.LastIndexOf(value));
        }

        /// <summary>
        /// Checks whether string is null, empty or consists of whitespace chars.
        /// </summary>
        public static bool IsNullEmptyOrWhiteSpace (string content)
        {
            if (String.IsNullOrEmpty(content))
                return true;

            return String.IsNullOrEmpty(content.TrimFull());
        }

        /// <summary>
        /// Performs <see cref="string.Trim()"/> additionally removing any BOM and other service symbols.
        /// </summary>
        public static string TrimFull (this string source)
        {
            #if UNITY_WEBGL // WebGL build under .NET 4.6 fails when using Trim with UTF-8 chars. (should be fixed in Unity 2018.1)
            var whitespaceChars = new System.Collections.Generic.List<char> {
                '\u0009','\u000A','\u000B','\u000C','\u000D','\u0020','\u0085','\u00A0',
                '\u1680','\u2000','\u2001','\u2002','\u2003','\u2004','\u2005','\u2006',
                '\u2007','\u2008','\u2009','\u200A','\u2028','\u2029','\u202F','\u205F',
                '\u3000','\uFEFF','\u200B',
            };

            // Trim start.
            if (string.IsNullOrEmpty(source)) return source;
            var c = source[0];
            while (whitespaceChars.Contains(c))
            {
                if (source.Length <= 1) return string.Empty;
                source = source.Substring(1);
                c = source[0];
            }

            // Trim end.
            if (string.IsNullOrEmpty(source)) return source;
            c = source[source.Length - 1];
            while (whitespaceChars.Contains(c))
            {
                if (source.Length <= 1) return string.Empty;
                source = source.Substring(0, source.Length - 1);
                c = source[source.Length - 1];
            }

            return source;
            #else
            return source.Trim().Trim(new char[] { '\uFEFF', '\u200B' });
            #endif
        }

        /// <summary>
        /// Given a file size (length in bytes), produces a human-readable string.
        /// </summary>
        /// <param name="size">Bytes length of the file.</param>
        /// <param name="unit">Minimum unit to use: { "B", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" }.</param>
        public static string FormatFileSize (double size, int unit = 0)
        {
            string[] units = { "B", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };

            while (size >= 1024)
            {
                size /= 1024;
                ++unit;
            }

            return string.Format("{0:G4} {1}", size, units[unit]);
        }
    }
}
