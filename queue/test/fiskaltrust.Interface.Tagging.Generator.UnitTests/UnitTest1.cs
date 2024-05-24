using System.Reflection;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace fiskaltrust.Interface.Tagging.Generator.UnitTests;

public class Tests
{
    [Fact]
    public void GeneratesEnumExtensionsCorrectly()
    {
        // The source code to test
        var source = """
            using fiskaltrust.Interface.Tagging.Generator;

            namespace TestNamespace
            {
                using TestNamespace.Extensions;

                public class TestClass {
                    public long TestField { get; set; }
                }

                [FlagExtensions(OnType = typeof(TestClass), OnField = nameof(TestClass.TestField))]
                public enum TestFlags : long
                {
                    Lel = 0x0000_0000_0004_0000,
                }

                [CaseExtensions(OnType = typeof(TestClass), OnField = nameof(TestClass.TestField)), Mask = 0x0000_0000_FFFF_0000, Shift = 4, CaseName = "TestCases"]
                public enum TestCases : long
                {
                    Lel = 0x0000_0000_1234_0000,
                }
            }
            """;
        var inputCompilation = CreateCompilation(source);
        var generator = new FlagExtensionGenerator();

        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);


        driver = driver.RunGeneratorsAndUpdateCompilation(inputCompilation, out var outputCompilation, out var diagnostics);
        diagnostics.Should().BeEmpty();

        var runResult = driver.GetRunResult();


    }

    private static Compilation CreateCompilation(string source)
    => CSharpCompilation.Create("compilation",
        new[] { CSharpSyntaxTree.ParseText(source) },
        new[] { MetadataReference.CreateFromFile(typeof(Binder).GetTypeInfo().Assembly.Location) },
        new CSharpCompilationOptions(OutputKind.ConsoleApplication));
}
