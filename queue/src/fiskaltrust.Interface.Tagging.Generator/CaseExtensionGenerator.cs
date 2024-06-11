using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;



namespace fiskaltrust.Interface.Tagging.Generator;
public readonly record struct CasesToGenerate : IToGenerate
{
    public readonly INamedTypeSymbol OnType;
    public readonly string OnField;
    public readonly string Namespace;
    public readonly string Name;
    public readonly long? Mask;
    public readonly int? Shift;
    public readonly string? CaseName;
    public readonly List<string> Members;

    public CasesToGenerate(string nameSpace, string name, INamedTypeSymbol onType, string onField, List<string> members, long? mask, int shift, string? caseName)
    {
        OnType = onType;
        OnField = onField;
        Members = members;
        Namespace = nameSpace;
        Name = name;
        Mask = mask;
        Shift = shift;
        CaseName = caseName;
    }

    public string GetSourceFileName() => $"{Namespace}.{OnType}{OnField}{CaseName}Ext.g.c";
}


public class CasesToGenerateFactory : IToGenerateFactory<CasesToGenerate>
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

            if (namedArgument.Key == "Mask"
                && (long?) namedArgument.Value.Value is { } m)
            {
                Properties.Add("Mask", m);
            }

            if (namedArgument.Key == "Shift"
                && (int?) namedArgument.Value.Value is { } s)
            {
                Properties.Add("Shift", s);
            }

            if (namedArgument.Key == "CaseName"
                && namedArgument.Value.Value?.ToString() is { } c)
            {
                Properties.Add("CaseName", c);
            }
        }
    }

    public CasesToGenerate Create(string nameSpace, string name, List<string> members)
    {
        var onType = (INamedTypeSymbol) Properties["OnType"];
        var onField = (string) Properties["OnField"];
        long? mask = Properties.TryGetValue("Mask", out var m) ? (long) m : throw new ArgumentException("Mask is required");
        var shift = Properties.TryGetValue("Shift", out var s) ? (int) s : 0;
        var caseName = Properties.TryGetValue("CaseName", out var c) ? (string) c : "Case";

        return new CasesToGenerate(nameSpace, name, onType, onField, members, mask, shift, caseName);
    }
}

[Generator]
public class CaseExtensionGenerator : ExtensionGenerator<CasesToGenerateFactory, CasesToGenerate>
{
    private const string ATTRIBUTE_NAME = "CaseExtensionsAttribute";
    private const string ATTRIBUTE_NAMESPACE = "fiskaltrust.Interface.Tagging.Generator";


    private const string _ATTRIBUTE_TEXT = $$"""
            namespace {{ATTRIBUTE_NAMESPACE}}
            {
                [global::System.AttributeUsage(global::System.AttributeTargets.Enum, Inherited = false, AllowMultiple = false)]
                [System.Diagnostics.Conditional("CaseExtensionGenerator_DEBUG")]
                public sealed class {{ATTRIBUTE_NAME}} : global::System.Attribute
                {
                    public {{ATTRIBUTE_NAME}}()
                    {
                    }
                    public global::System.Type OnType { get; set; }
                    public string OnField { get; set; }
                    public long Mask { get; set; }
                    public int Shift { get; set; }
                    public string CaseName { get; set; }
                }
            }
            """;

    public CaseExtensionGenerator() : base(ATTRIBUTE_NAME, ATTRIBUTE_NAMESPACE, _ATTRIBUTE_TEXT)
    {
    }

    internal override string GenerateExtensionClass(CasesToGenerate enumToGenerate)
    {
        return $$"""
                namespace {{enumToGenerate.Namespace}}.Extensions
                {
                    public static class {{enumToGenerate.OnType.Name}}{{enumToGenerate.OnField}}{{enumToGenerate.CaseName}}Ext {
                        public static long Get{{enumToGenerate.CaseName}}(this {{enumToGenerate.OnType.ContainingNamespace}}.{{enumToGenerate.OnType.Name}} value) => (value.{{enumToGenerate.OnField}} & 0x{{enumToGenerate.Mask:X}}L) >> (4 * {{enumToGenerate.Shift}});

                        {{string.Join("\n        ", enumToGenerate.Members.Select(member => $"""
                        public static bool Is{member}(this {enumToGenerate.OnType.ContainingNamespace}.{enumToGenerate.OnType.Name} value) => ((value.{enumToGenerate.OnField} & 0x{enumToGenerate.Mask:X}) >> (4 * {enumToGenerate.Shift})) == ((long)global::{enumToGenerate.Namespace}.{enumToGenerate.Name}.{member});
                        """))}}

                       public static void Set{{enumToGenerate.CaseName}}(this {{enumToGenerate.OnType.ContainingNamespace}}.{{enumToGenerate.OnType.Name}} value, long data) { (value.{{enumToGenerate.OnField}} = (value.{{enumToGenerate.OnField}} & ~0x{{enumToGenerate.Mask:X}}L) | ((ulong) data << (4 * {{enumToGenerate.Shift}}))  };
                    }
                }
                """;
    }
}
