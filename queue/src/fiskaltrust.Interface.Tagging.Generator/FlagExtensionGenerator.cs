using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;



namespace fiskaltrust.Interface.Tagging.Generator
{
    public readonly record struct EnumToGenerate
    {
        public readonly INamedTypeSymbol OnType;
        public readonly string OnField;
        public readonly string Namespace;
        public readonly List<(string name, long mask)> Members;

        public EnumToGenerate(string nameSpace, INamedTypeSymbol onType, string onField, List<(string name, long mask)> members)
        {
            OnType = onType;
            OnField = onField;
            Members = members;
            Namespace = nameSpace;
        }
    }

    [Generator]
    public class FlagExtensionGenerator : IIncrementalGenerator
    {
        private const string ATTRIBUTE_NAME = "FlagExtensionsAttribute";
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
                }
            }
            """;


        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            context.RegisterPostInitializationOutput((ctx) => ctx.AddSource("FlagExtensionsAttribute.g.cs", _ATTRIBUTE_TEXT));

            var enumsToGenerate = context.SyntaxProvider
                .ForAttributeWithMetadataName(
                    ATTRIBUTE_FULL_NAME,
                    predicate: (node, _) => node is EnumDeclarationSyntax,
                    transform: GetTypeToGenerate)
                .Where(static m => m is not null)
                .Select(static (m, _) => m!.Value);

            context.RegisterSourceOutput(enumsToGenerate, static (spc, enumToGenerate) => Execute(in enumToGenerate, spc));
        }

        private static void Execute(in EnumToGenerate enumToGenerate, SourceProductionContext context)
        {
            var result = GenerateExtensionClass(enumToGenerate);
            context.AddSource(enumToGenerate.OnType + "Ext.g.cs", SourceText.From(result, Encoding.UTF8));
        }

        public static EnumToGenerate? GetTypeToGenerate(GeneratorAttributeSyntaxContext context, CancellationToken ct)
        {
            if (context.TargetSymbol is not INamedTypeSymbol enumSymbol)
            {
                // nothing to do if this type isn't available
                return null;
            }

            ct.ThrowIfCancellationRequested();

            INamedTypeSymbol? onType = null;
            string? onField = null;

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

                    if (namedArgument.Key == "OnField"
                        && namedArgument.Value.Value?.ToString() is { } of)
                    {
                        onField = of;
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

            return TryExtractEnumSymbol(enumSymbol, onType, onField);
        }


        private static EnumToGenerate? TryExtractEnumSymbol(INamedTypeSymbol enumSymbol, INamedTypeSymbol onType, string onField)
        {
            var enumMembers = enumSymbol.GetMembers();
            var members = new List<(string, long)>(enumMembers.Length);
            var nameSpace = enumSymbol.ContainingNamespace.IsGlobalNamespace ? string.Empty : enumSymbol.ContainingNamespace.ToString();

            foreach (var member in enumMembers)
            {
                if (member is not IFieldSymbol field
                    || field.ConstantValue is null)
                {
                    continue;
                }

                members.Add((member.Name, (long) field.ConstantValue));
            }

            return new EnumToGenerate(nameSpace, onType, onField, members);
        }

        public static string GenerateExtensionClass(EnumToGenerate enumToGenerate)
        {
            return $$"""
                namespace {{enumToGenerate.Namespace}}.Extensions
                {
                    public static class {{enumToGenerate.OnType.Name}}Ext {
                        {{string.Join("\n", enumToGenerate.Members.Select(member => $"""
                                public static bool Is{member.name}(this {enumToGenerate.OnType.ContainingNamespace}.{enumToGenerate.OnType.Name} self) => (self.{enumToGenerate.OnField} & 0x{member.mask:X16}) > 0;
                                """))}}
                    }
                }
                """;
        }

    }
}