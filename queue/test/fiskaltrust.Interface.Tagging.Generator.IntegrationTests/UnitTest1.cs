
using Xunit;
using fiskaltrust.Interface.Tagging.Generator;
using fiskaltrust.Interface.Tagging.Generator.IntegrationTests.Extensions;

namespace fiskaltrust.Interface.Tagging.Generator.IntegrationTests;

public class TestClass
{
    public long TestField { get; set; }
}

public class Tests
{
    [FlagExtensions(OnType = typeof(TestClass), OnField = nameof(TestClass.TestField))]
    public enum TestFlags : long
    {
        Lel = 0x0000_0000_0004_0000,
    }

    [Fact]
    public void TestRun()
    {
        var test = new TestClass { TestField = 0 }.IsLel();
    }
}
