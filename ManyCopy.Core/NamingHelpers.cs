using System;
using System.IO;
using System.Linq;

namespace ManyCopy.Core
{
    public static class NamingHelpers
    {
        /// <summary>
        /// Sanitizes a filename segment by removing characters invalid for file names.
        /// Returns an empty string if input is null or whitespace.
        /// </summary>
        public static string SanitizeFileSegment(string? text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;
            var invalid = Path.GetInvalidFileNameChars();
            var filtered = new string(text.Where(ch => !invalid.Contains(ch)).ToArray());
            return filtered.Trim();
        }

        public static int CalculateRangePadWidth(string? startText, string? endText)
        {
            static int WidthFrom(string? text)
            {
                if (string.IsNullOrWhiteSpace(text)) return 0;
                var trimmed = text.Trim();
                if (trimmed.Length == 0) return 0;

                bool hasSign = trimmed[0] == '-' || trimmed[0] == '+';
                if (hasSign)
                {
                    trimmed = trimmed[1..];
                }

                if (trimmed.Length <= 1) return 0;
                if (!trimmed.All(char.IsDigit)) return 0;
                if (trimmed[0] != '0') return 0;

                return trimmed.Length;
            }

            return Math.Max(WidthFrom(startText), WidthFrom(endText));
        }

        public static string FormatRangeNumber(int value, int padWidth)
        {
            if (padWidth > 0)
            {
                return value.ToString($"D{padWidth}");
            }

            return value.ToString();
        }

        /// <summary>
        /// Compose a target file name from the base name and optional prefix/suffix.
        /// Allows optional padding and separators for numbered prefix/suffix.
        /// </summary>
        public static string BuildTargetName(
            string baseName,
            bool useFixed, string? fixedPrefix,
            bool useRange, string? rangeBase, int index,
            bool useSuffix, string? suffix,
            int prefixPadWidth = 0,
            int suffixPadWidth = 0,
            string? prefixSeparator = null,
            string? suffixSeparator = null)
        {
            string prefixPart = string.Empty;
            if (useRange)
            {
                var basePart = SanitizeFileSegment(rangeBase);
                var num = FormatRangeNumber(index, prefixPadWidth);
                prefixPart = basePart + num;
            }
            else if (useFixed)
            {
                prefixPart = SanitizeFileSegment(fixedPrefix);
            }

            string nameNoExt = Path.GetFileNameWithoutExtension(baseName);
            string ext = Path.GetExtension(baseName);
            string suffixPart = string.Empty;
            if (useSuffix)
            {
                var sufBase = SanitizeFileSegment(suffix);
                // When suffixPadWidth > 0 and suffix contains a trailing number placeholder,
                // the caller should have already provided the numeric string. We still allow
                // pure text suffixes here.
                suffixPart = sufBase;
            }

            // Insert separators only when the corresponding part is non-empty
            var preSep = string.IsNullOrEmpty(prefixPart) ? string.Empty : (prefixSeparator ?? string.Empty);
            var sufSep = string.IsNullOrEmpty(suffixPart) ? string.Empty : (suffixSeparator ?? string.Empty);

            if (!string.IsNullOrEmpty(suffixPart)) nameNoExt += sufSep + suffixPart;

            return prefixPart + (string.IsNullOrEmpty(prefixPart) ? string.Empty : preSep) + nameNoExt + ext;
        }

        // Back-compat overload keeping the original signature used by Program.cs before padding/separators
        public static string BuildTargetName(
            string baseName,
            bool useFixed, string? fixedPrefix,
            bool useRange, string? rangeBase, int index,
            bool useSuffix, string? suffix)
            => BuildTargetName(baseName, useFixed, fixedPrefix, useRange, rangeBase, index, useSuffix, suffix, 0, 0, null, null);
    }
}
