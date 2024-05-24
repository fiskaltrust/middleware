using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;



namespace fiskaltrust.Interface.Tagging.Generator
{
    public readonly record struct CasesToGenerate
    {
        public readonly INamedTypeSymbol OnType;
        public readonly string OnField;
        public readonly string Namespace;
        public readonly long? Mask;
        public readonly int? Shift;
        public readonly string? CaseName;
        public readonly List<(string name, long mask)> Members;

        public CasesToGenerate(string nameSpace, INamedTypeSymbol onType, string onField, List<(string name, long mask)> members, long? mask, int shift, string? caseName)
        {
            OnType = onType;
            OnField = onField;
            Members = members;
            Namespace = nameSpace;
            Mask = mask;
            Shift = shift;
            CaseName = caseName;
        }
    }

    [Generator]
    public class CaseExtensionGenerator : IIncrementalGenerator
    {
        private const string ATTRIBUTE_NAME = "CaseExtensionsAttribute";
        private const string ATTRIBUTE_NAMESPACE = "fiskaltrust.Interface.Tagging.Generator";
        private const string ATTRIBUTE_FULL_NAME = $"{ATTRIBUTE_NAMESPACE}.{ATTRIBUTE_NAME}";


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
                    public long Mask { get; set; }
                    public int Shift { get; set; }
                    public string CaseName { get; set; }
                }
            }
            """;


        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            context.RegisterPostInitializationOutput((ctx) => ctx.AddSource("CaseExtensionsAttribute.g.cs", _ATTRIBUTE_TEXT));

            var enumsToGenerate = context.SyntaxProvider
                .ForAttributeWithMetadataName(
                    ATTRIBUTE_FULL_NAME,
                    predicate: (node, _) => node is EnumDeclarationSyntax,
                    transform: GetTypeToGenerate)
                .Where(static m => m is not null)
                .Select(static (m, _) => m!.Value);

            context.RegisterSourceOutput(enumsToGenerate, static (spc, enumToGenerate) => Execute(in enumToGenerate, spc));
        }

        private static void Execute(in CasesToGenerate enumToGenerate, SourceProductionContext context)
        {
            var result = GenerateExtensionClass(enumToGenerate);
            context.AddSource($"{enumToGenerate.Namespace}.{enumToGenerate.OnType}{enumToGenerate.OnField}{enumToGenerate.CaseName}Ext.g.cs", SourceText.From(result, Encoding.UTF8));
        }

        public static CasesToGenerate? GetTypeToGenerate(GeneratorAttributeSyntaxContext context, CancellationToken ct)
        {
            if (context.TargetSymbol is not INamedTypeSymbol enumSymbol)
            {
                // nothing to do if this type isn't available
                return null;
            }

            ct.ThrowIfCancellationRequested();

            INamedTypeSymbol? onType = null;
            string? onField = null;
            long? mask = null;
            var shift = 0;
            var caseName = "Case";

            foreach (var attributeData in enumSymbol.GetAttributes())
            {
                if (attributeData.AttributeClass?.Name != ATTRIBUTE_NAME ||
                    attributeData.AttributeClass.ToDisplayString() != ATTRIBUTE_FULL_NAME)
                {
                    continue;
                }

                foreach (var namedArgument in attributeData.NamedArguments)
                {
                    if (namedArgument.Key == "OnType"
                        && ((INamedTypeSymbol?) namedArgument.Value.Value) is { } ot)
                    {
                        onType = ot;
                        continue;
                    }

                    if (namedArgument.Key == "Mask"
                        && (long?) namedArgument.Value.Value is { } m)
                    {
                        mask = m;
                    }

                    if (namedArgument.Key == "Shift"
                        && (int?) namedArgument.Value.Value is { } s)
                    {
                        shift = s;
                    }

                    if (namedArgument.Key == "OnField"
                        && namedArgument.Value.Value?.ToString() is { } of)
                    {
                        onField = of;
                    }

                    if (namedArgument.Key == "CaseName"
                        && namedArgument.Value.Value?.ToString() is { } c)
                    {
                        caseName = c;
                    }
                }
            }

            if (onType is null)
            {
                throw new InvalidOperationException($"Missing required arguments OnType on {enumSymbol.Name}");
            }

            if (onField is null)
            {
                throw new InvalidOperationException($"Missing required arguments OnField on {enumSymbol.Name}");
            }

            return TryExtractEnumSymbol(enumSymbol, onType, onField, mask, shift, caseName);
        }


        private static CasesToGenerate? TryExtractEnumSymbol(INamedTypeSymbol enumSymbol, INamedTypeSymbol onType, string onField, long? mask, int shift, string caseName)
        {
            var enumMembers = enumSymbol.GetMembers();
            var members = new List<(string, long)>(enumMembers.Length);
            var nameSpace = enumSymbol.ContainingNamespace.IsGlobalNamespace ? string.Empty : enumSymbol.ContainingNamespace.ToString() ?? string.Empty;

            foreach (var member in enumMembers)
            {
                if (member is not IFieldSymbol field
                    || field.ConstantValue is null)
                {
                    continue;
                }

                members.Add((member.Name, (long) field.ConstantValue));
            }

            return new CasesToGenerate(nameSpace, onType, onField, members, mask, shift, caseName);
        }

        public static string GenerateExtensionClass(CasesToGenerate enumToGenerate)
        {
            return $$"""
                namespace {{enumToGenerate.Namespace}}.Extensions
                {
                    public static class {{enumToGenerate.OnType.Name}}{{enumToGenerate.OnField}}{{enumToGenerate.CaseName}}Ext {
                        public static long Get{{enumToGenerate.CaseName}}(this {{enumToGenerate.OnType.ContainingNamespace}}.{{enumToGenerate.OnType.Name}} value) => (value.{{enumToGenerate.OnField}} & 0x{{enumToGenerate.Mask:X}}L) >> (4 * {{enumToGenerate.Shift}});

                        {{string.Join("\n        ", enumToGenerate.Members.Select(member => $"""
                        public static bool Is{member.name}(this {enumToGenerate.OnType.ContainingNamespace}.{enumToGenerate.OnType.Name} value) => ((value.{enumToGenerate.OnField} & 0x{enumToGenerate.Mask:X}) >> (4 * {enumToGenerate.Shift})) == 0x{member.mask:X16};
                        """))}}

                        {{string.Join("\n        ", enumToGenerate.Members.Select(member => $$"""
                        public static void Set{{member.name}}(this {{enumToGenerate.OnType.ContainingNamespace}}.{{enumToGenerate.OnType.Name}} value) { value.{{enumToGenerate.OnField}} = (~0x{{enumToGenerate.Mask:X}}L & value.{{enumToGenerate.OnField}}) | (0x{{member.mask:X16}} << (4 * {{enumToGenerate.Shift}})); }
                        """))}}
                    }
                }
                """;
        }

    }
}