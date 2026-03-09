namespace Mitsuoshie.Core.Tests.Monitoring;

using Mitsuoshie.Core.Monitoring;

public class AccessMaskParserTests
{
    [Theory]
    [InlineData("0x1", true, 0x1)]
    [InlineData("0x80", true, 0x80)]
    [InlineData("0x10000", true, 0x10000)]
    [InlineData("0x10002", true, 0x10002)]
    [InlineData("0x80000000", true, unchecked((int)0x80000000))] // GENERIC_READ（bit31）
    [InlineData("0xFFFFFFFF", true, unchecked((int)0xFFFFFFFF))] // 全ビット
    [InlineData("128", true, 128)]
    [InlineData("", false, 0)]
    [InlineData("invalid", false, 0)]
    public void TryParse_ReturnsExpected(string input, bool expectedResult, int expectedMask)
    {
        var result = AccessMaskParser.TryParse(input, out var mask);

        Assert.Equal(expectedResult, result);
        if (expectedResult)
            Assert.Equal(expectedMask, mask);
    }
}
