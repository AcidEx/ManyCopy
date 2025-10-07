using System;
using System.IO;
using System.Linq;

namespace ManyCopy.Core
{
    public static class NamingHelpers
    {
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

        public static string BuildTargetName(
            string baseName,
            bool useFixed, string? fixedPrefix,
            bool useRange, string? rangeBase, int index,
            bool useSuffix, string? suffix)
        {
            string prefixPart = string.Empty;
            if (useRange)
            {
                prefixPart = (rangeBase ?? string.Empty).Trim() + index.ToString();
            }
            else if (useFixed)
            {
                prefixPart = (fixedPrefix ?? string.Empty).Trim();
            }

            string nameNoExt = Path.GetFileNameWithoutExtension(baseName);
            string ext = Path.GetExtension(baseName);
            string suffixPart = useSuffix ? (suffix ?? string.Empty).Trim() : string.Empty;
            if (!string.IsNullOrEmpty(suffixPart)) nameNoExt += suffixPart;

            return prefixPart + nameNoExt + ext;
        }
    }
}
