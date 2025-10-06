using System;
using System.Reflection;
using ManyCopy;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ManyCopy.Tests
{
    [TestClass]
    public class BuildTargetNameTests
    {
        private static readonly MethodInfo BuildTargetNameMethod = typeof(MainForm)
            .GetMethod("BuildTargetName", BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("BuildTargetName method not found");

        private static readonly MethodInfo CalculateRangePadWidthMethod = typeof(MainForm)
            .GetMethod("CalculateRangePadWidth", BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("CalculateRangePadWidth method not found");

        private static readonly MethodInfo FormatRangeNumberMethod = typeof(MainForm)
            .GetMethod("FormatRangeNumber", BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("FormatRangeNumber method not found");

        private static string InvokeBuildTargetName(
            string baseName,
            bool useFixed, string? fixedPrefix,
            bool useRange, string? rangeBase, int index,
            bool useSuffix, string? suffix)
        {
            return (string)BuildTargetNameMethod.Invoke(
                obj: null,
                parameters: new object?[]
                {
                    baseName,
                    useFixed, fixedPrefix,
                    useRange, rangeBase, index,
                    useSuffix, suffix
                })!;
        }

        private static int InvokeCalculateRangePadWidth(string start, string end)
        {
            return (int)CalculateRangePadWidthMethod.Invoke(null, new object?[] { start, end })!;
        }

        private static string InvokeFormatRangeNumber(int value, int padWidth)
        {
            return (string)FormatRangeNumberMethod.Invoke(null, new object?[] { value, padWidth })!;
        }

        [TestMethod]
        public void FixedPrefix_IsAppliedWithoutWhitespace()
        {
            var result = InvokeBuildTargetName(
                baseName: "report.docx",
                useFixed: true,
                fixedPrefix: " Project_ ",
                useRange: false,
                rangeBase: null,
                index: 0,
                useSuffix: false,
                suffix: null);

            Assert.AreEqual("Project_report.docx", result);
        }

        [TestMethod]
        public void RangeHelper_RespectsLeadingZerosWhenPresent()
        {
            int padWidth = InvokeCalculateRangePadWidth("001", "010");
            var formatted = InvokeFormatRangeNumber(1, padWidth);

            Assert.AreEqual("001", formatted);
        }

        [TestMethod]
        public void RangeHelper_SkipsPaddingWhenNotNeeded()
        {
            int padWidth = InvokeCalculateRangePadWidth("3", "12");
            var formatted = InvokeFormatRangeNumber(7, padWidth);

            Assert.AreEqual("7", formatted);
        }

        [TestMethod]
        public void RangePrefix_AppendsIndexToTrimmedBase()
        {
            var result = InvokeBuildTargetName(
                baseName: "image.png",
                useFixed: false,
                fixedPrefix: null,
                useRange: true,
                rangeBase: " Batch ",
                index: 12,
                useSuffix: false,
                suffix: null);

            Assert.AreEqual("Batch12image.png", result);
        }

        [TestMethod]
        public void Suffix_IsInsertedBeforeExtension()
        {
            var result = InvokeBuildTargetName(
                baseName: "archive.zip",
                useFixed: false,
                fixedPrefix: null,
                useRange: false,
                rangeBase: null,
                index: 0,
                useSuffix: true,
                suffix: "_final");

            Assert.AreEqual("archive_final.zip", result);
        }
    }
}
