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
    private readonly string _attributeName = "CaseExtensionsAttribute";
    private readonly string _attributeNamespace = "fiskaltrust.Interface.Tagging.Generator";
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

        var (nameSpace, members) = TryExtractEnumSymbol(enumSymbol);
        return factory.Create(nameSpace, members);
    }

    private static (string, List<(string, long)>) TryExtractEnumSymbol(INamedTypeSymbol enumSymbol)
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

        return new(nameSpace, members);
    }

    internal abstract string GenerateExtensionClass(T enumToGenerate);
}

