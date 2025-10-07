using ManyCopy.Core;
using Xunit;

namespace ManyCopy.Tests
{
    public class NamingHelpersTests
    {
        [Theory]
        [InlineData("001", "010", 3)]
        [InlineData("1", "2", 0)]
        [InlineData("0005", "0007", 4)]
        [InlineData("-01", "-05", 2)]
        [InlineData("001", "", 3)]
        public void CalculateRangePadWidth_DetectsExpectedWidth(string start, string end, int expected)
        {
            int pad = NamingHelpers.CalculateRangePadWidth(start, end);
            Assert.Equal(expected, pad);
        }

        [Theory]
        [InlineData(1, 0, "1")]
        [InlineData(5, 3, "005")]
        [InlineData(42, 4, "0042")]
        public void FormatRangeNumber_AppliesPadding(int value, int padWidth, string expected)
        {
            string formatted = NamingHelpers.FormatRangeNumber(value, padWidth);
            Assert.Equal(expected, formatted);
        }

        [Fact]
        public void BuildTargetName_ComposesExpectedName()
        {
            string baseName = "file.txt";
            string result = NamingHelpers.BuildTargetName(
                baseName,
                useFixed: false,
                fixedPrefix: null,
                useRange: true,
                rangeBase: "copy-",
                index: 7,
                useSuffix: true,
                suffix: "_final");

            Assert.Equal("copy-7file_final.txt", result);
        }

        [Fact]
        public void BuildTargetName_UsesFixedPrefixWhenRequested()
        {
            string result = NamingHelpers.BuildTargetName(
                baseName: "image.png",
                useFixed: true,
                fixedPrefix: "prefix-",
                useRange: false,
                rangeBase: null,
                index: 0,
                useSuffix: false,
                suffix: null);

            Assert.Equal("prefix-image.png", result);
        }
    }
}
