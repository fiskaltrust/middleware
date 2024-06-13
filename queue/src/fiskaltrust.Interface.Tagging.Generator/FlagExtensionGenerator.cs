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
    public readonly string Name;
    public readonly string? CaseName;
    public readonly string? Prefix;
    public readonly List<string> Members;

    public FlagsToGenerate(string nameSpace, string name, INamedTypeSymbol onType, string onField, List<string> members, string? caseName, string? prefix)
    {
        OnType = onType;
        OnField = onField;
        Members = members;
        Namespace = nameSpace;
        Name = name;
        CaseName = caseName;
        Prefix = prefix;
    }

    public string GetSourceFileName() => $"{Namespace}.{Prefix}{OnType}{OnField}{CaseName}FlagExt.g.cs";
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

            if (namedArgument.Key == "CaseName"
                && namedArgument.Value.Value?.ToString() is { } c)
            {
                Properties.Add("CaseName", c);
            }

            if (namedArgument.Key == "Prefix"
                    && namedArgument.Value.Value?.ToString() is { } p)
            {
                Properties.Add("Prefix", p);
            }
        }
    }

    public FlagsToGenerate Create(string nameSpace, string name, List<string> members)
    {
        var onType = (INamedTypeSymbol) Properties["OnType"];
        var onField = (string) Properties["OnField"];
        var caseName = Properties.TryGetValue("CaseName", out var c) ? (string) c : "Case";
        var prefix = Properties.TryGetValue("Prefix", out var p) ? (string) p : "";
        return new FlagsToGenerate(nameSpace, name, onType, onField, members, caseName, prefix);
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
                    public string CaseName { get; set; }
                    public string Prefix { get; set; }
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
                    public static class {{enumToGenerate.Prefix}}{{enumToGenerate.OnType.Name}}{{enumToGenerate.OnField}}{{enumToGenerate.CaseName}}FlagExt {
                        {{string.Join("\n        ", enumToGenerate.Members.Select(member => $"""
                                public static bool Is{enumToGenerate.Prefix}{enumToGenerate.CaseName}{member}(this {enumToGenerate.OnType.ContainingNamespace}.{enumToGenerate.OnType.Name} value) => (value.{enumToGenerate.OnField} & ((long)global::{enumToGenerate.Namespace}.{enumToGenerate.Name}.{member})) > 0;
                                """))}}

                        {{string.Join("\n        ", enumToGenerate.Members.Select(member => $$"""
                                public static void Set{{enumToGenerate.Prefix}}{{enumToGenerate.CaseName}}{{member}}(this {{enumToGenerate.OnType.ContainingNamespace}}.{{enumToGenerate.OnType.Name}} value) { value.{{enumToGenerate.OnField}} = (value.{{enumToGenerate.OnField}} | ((long)global::{{enumToGenerate.Namespace}}.{{enumToGenerate.Name}}.{{member}})); }
                                """))}}
                    }
                }
                """;
    }

}
