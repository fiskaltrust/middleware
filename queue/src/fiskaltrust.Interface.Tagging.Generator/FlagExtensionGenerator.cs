using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace fiskaltrust.Interface.Tagging.Generator;

public readonly record struct FlagsToGenerate : IToGenerate
{
    public readonly INamedTypeSymbol OnType;
    public readonly string OnField;
    public readonly string Namespace;
    public readonly List<(string name, long mask)> Members;

    public FlagsToGenerate(string nameSpace, INamedTypeSymbol onType, string onField, List<(string name, long mask)> members)
    {
        OnType = onType;
        OnField = onField;
        Members = members;
        Namespace = nameSpace;
    }

    public string GetSourceFileName() => $"{Namespace}.{OnType}.{OnField}FlagExt.g.cs";
}

public class FlagsToGenerateFactory : IToGenerateFactory<FlagsToGenerate>
{
    private Dictionary<string, object> Properties { get; set; } = new();

    public void AddProperties(ImmutableArray<KeyValuePair<string, TypedConstant>> namedArguments)
    {
        foreach (var namedArgument in namedArguments)
        {
            if (namedArgument.Key == "OnType"
                                && ((INamedTypeSymbol?) namedArgument.Value.Value) is { } ot)
            {
                Properties.Add("OnType", ot);
                continue;
            }

            if (namedArgument.Key == "OnField"
                && namedArgument.Value.Value?.ToString() is { } of)
            {
                Properties.Add("OnField", of);
            }
        }
    }

    public FlagsToGenerate Create(string nameSpace, List<(string, long)> members)
    {
        var onType = (INamedTypeSymbol) Properties["OnType"];
        var onField = (string) Properties["OnField"];
        return new FlagsToGenerate(nameSpace, onType, onField, members);
    }
}

[Generator]
public class FlagExtensionGenerator : ExtensionGenerator<FlagsToGenerateFactory, FlagsToGenerate>
{
    private const string ATTRIBUTE_NAME = "FlagExtensionsAttribute";
    private const string ATTRIBUTE_NAMESPACE = "fiskaltrust.Interface.Tagging.Generator";

    private const string _ATTRIBUTE_TEXT = $$"""
            namespace {{ATTRIBUTE_NAMESPACE}}
            {
                [global::System.AttributeUsage(global::System.AttributeTargets.Enum, Inherited = false, AllowMultiple = false)]
                [System.Diagnostics.Conditional("FlagExtensionGenerator_DEBUG")]
                public sealed class {{ATTRIBUTE_NAME}} : global::System.Attribute
                {
                    public {{ATTRIBUTE_NAME}}()
                    {
                    }
                    public global::System.Type OnType { get; set; }
                    public string OnField { get; set; }
                }
            }
            """;

    public FlagExtensionGenerator() : base(ATTRIBUTE_NAME, ATTRIBUTE_NAMESPACE, _ATTRIBUTE_TEXT)
    {
    }

    internal override string GenerateExtensionClass(FlagsToGenerate enumToGenerate)
    {
        return $$"""
                namespace {{enumToGenerate.Namespace}}.Extensions
                {
                    public static class {{enumToGenerate.OnType.Name}}FlagExt {
                        {{string.Join("\n        ", enumToGenerate.Members.Select(member => $"""
                                public static bool Is{member.name}(this {enumToGenerate.OnType.ContainingNamespace}.{enumToGenerate.OnType.Name} value) => (value.{enumToGenerate.OnField} & 0x{member.mask:X16}) > 0;
                                """))}}

                        {{string.Join("\n        ", enumToGenerate.Members.Select(member => $$"""
                                public static void Set{{member.name}}(this {{enumToGenerate.OnType.ContainingNamespace}}.{{enumToGenerate.OnType.Name}} value) { value.{{enumToGenerate.OnField}} = (value.{{enumToGenerate.OnField}} | 0x{{member.mask:X16}}); }
                                """))}}
                    }
                }
                """;
    }

}
