
using Xunit;
using fiskaltrust.Interface.Tagging.Generator;
using fiskaltrust.Interface.Tagging.Generator.IntegrationTests.Extensions;
using FluentAssertions;

namespace fiskaltrust.Interface.Tagging.Generator.IntegrationTests;

public class TestClass
{
    public long TestField { get; set; }
}

[FlagExtensions(OnType = typeof(TestClass), OnField = nameof(TestClass.TestField))]
public enum TestFlags : long
{
    Lel = 0x0000_0004_0000_0000,
}

[CaseExtensions(OnType = typeof(TestClass), OnField = nameof(TestClass.TestField), Mask = 0x0000_0000_FFFF_0000, Shift = 4, CaseName = "TestCases")]
public enum TestCases : long
{
    LelCase = 0x0004,
}

public class Tests
{
    [Fact]
    public void TestRun()
    {
        var test = new TestClass { TestField = 0x1234_0000_0000_0000 };
        test.IsLel().Should().BeFalse();
        test.SetLel();
        test.IsLel().Should().BeTrue();

        test.IsLelCase().Should().BeFalse();
        test.SetLelCase();
        test.IsLelCase().Should().BeTrue();

        test.GetTestCases().Should().Be(0x0004);
    }
}
