using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace fiskaltrust.Interface.Tagging.Generator;

public abstract class ExtensionGenerator<F, T> : IIncrementalGenerator where F : IToGenerateFactory<T>, new() where T : IToGenerate
{
    private readonly string _attributeName;
    private readonly string _attributeNamespace;
    private string _attributeFullName => $"{_attributeNamespace}.{_attributeName}";

    private readonly string _attributeText;

    protected ExtensionGenerator(string attributeName, string attributeNamespace, string attributeText)
    {
        _attributeName = attributeName;
        _attributeNamespace = attributeNamespace;
        _attributeText = attributeText;
    }

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput((ctx) => ctx.AddSource($"{_attributeName}.g.cs", _attributeText));

        var toGenerate = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                _attributeFullName,
                predicate: (node, _) => node is EnumDeclarationSyntax,
                transform: GetTypeToGenerate)
            .Where(static m => m is not null)
            .Select(static (m, _) => m!);

        context.RegisterSourceOutput(toGenerate, (spc, toGenerate) => Execute(in toGenerate, spc));
    }

    private void Execute(in T toGenerate, SourceProductionContext context)
    {
        var result = GenerateExtensionClass(toGenerate);
        context.AddSource(toGenerate.GetSourceFileName(), SourceText.From(result, Encoding.UTF8));
    }

    private T? GetTypeToGenerate(GeneratorAttributeSyntaxContext context, CancellationToken ct)
    {
        if (context.TargetSymbol is not INamedTypeSymbol enumSymbol)
        {
            // nothing to do if this type isn't available
            return default;
        }

        ct.ThrowIfCancellationRequested();


        var factory = new F();

        foreach (var attributeData in enumSymbol.GetAttributes())
        {
            if (attributeData.AttributeClass?.Name != _attributeName ||
                attributeData.AttributeClass.ToDisplayString() != _attributeFullName)
            {
                continue;
            }

            factory.AddProperties(attributeData.NamedArguments);
        }

        var (nameSpace, name, members) = TryExtractEnumSymbol(enumSymbol);
        return factory.Create(nameSpace, name, members);
    }

    private static (string, string, List<string>) TryExtractEnumSymbol(INamedTypeSymbol enumSymbol)
    {
        var enumMembers = enumSymbol.GetMembers();
        var members = new List<string>(enumMembers.Length);
        var nameSpace = enumSymbol.ContainingNamespace.IsGlobalNamespace ? string.Empty : enumSymbol.ContainingNamespace.ToString() ?? string.Empty;
        var name = enumSymbol.Name;
        foreach (var member in enumMembers)
        {
            if (member is not IFieldSymbol field
                || field.ConstantValue is null)
            {
                if (member.Kind == SymbolKind.Field)
                {
                    throw new InvalidOperationException($"Enum member {member.Name} must have a constant value");
                }
                continue;
            }

            members.Add(member.Name);
        }

        return new(nameSpace, name, members);
    }

    internal abstract string GenerateExtensionClass(T enumToGenerate);
}

