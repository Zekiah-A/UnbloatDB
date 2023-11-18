using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace UnbloatDB.Generators;

[Generator]
public class GroupGenerator : IIncrementalGenerator
{
    private const string GroupAttributeAssemblyName = "UnbloatDB.Attributes.GroupAttribute";
    private const string KeyReferenceAttributeAssemblyName = "UnbloatDB.Attributes.KeyReferenceAttribute";
    private const string ReferenceResolverAttributeAssemblyName = "UnbloatDB.Attributes.ReferenceResolverAttribute";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var classRecordDeclarations = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, token) => IsSyntaxTargetForGeneration(node),
                transform: static (syntaxContext, token) => GetSemanticTargetForGeneration(syntaxContext))
            .Where(static classSyntax => classSyntax is not null);
        
        var compilationAndClasses = (IncrementalValueProvider<(Compilation Compilation, ImmutableArray<OutboundReferences> OutboundReferences)>)
            context.CompilationProvider.Combine(classRecordDeclarations.Collect())!;
        
        context.RegisterSourceOutput(compilationAndClasses,
            static (sourceProductionContext, source) => Execute(source.Compilation, source.OutboundReferences, sourceProductionContext));
    }

    private static bool IsSyntaxTargetForGeneration(SyntaxNode node)
    {
        return node switch
        {
            RecordDeclarationSyntax { AttributeLists.Count: > 1 } => true,
            ClassDeclarationSyntax { AttributeLists.Count: > 1 } => true,
            _ => false
        };
    }

    private static OutboundReferences? GetSemanticTargetForGeneration(GeneratorSyntaxContext context)
    {
        var classSyntax = (TypeDeclarationSyntax)context.Node;

        foreach (var attributeListSyntax in classSyntax.AttributeLists)
        {
            foreach (var attributeSyntax in attributeListSyntax.Attributes)
            {
                var attributeSymbol = context.SemanticModel.GetSymbolInfo(attributeSyntax).Symbol;
                if (attributeSymbol == null)
                {
                    continue;
                }

                var attributeType = attributeSymbol.ContainingType;
                switch (attributeType.ToDisplayString())
                {
                    case GroupAttributeAssemblyName:
                    {
                        // Bingo -  We now map out all the groups that this group references with it's respective props
                        var references = new OutboundReferences(classSyntax);
                        foreach (var member in classSyntax.Members)
                        {
                            foreach (var memberAttribList in member.AttributeLists)
                            {
                                foreach (var memberAttribSyntax in memberAttribList.Attributes)
                                {
                                    var memberAttribSymbol =
                                        context.SemanticModel.GetSymbolInfo(memberAttribSyntax).Symbol;
                                    if (memberAttribSymbol == null)
                                    {
                                        continue;
                                    }

                                    var memberAttribType = attributeSymbol.ContainingType;
                                    if (memberAttribType.ToDisplayString() == KeyReferenceAttributeAssemblyName)
                                    {
                                        // Double bingo - we have found a key reference
                                        member.GetType().GetGenericTypeDefinition().ToString();
                                    }
                                }
                            }
                        }
                        break;
                    }
                    case KeyReferenceAttributeAssemblyName:
                    {
                        // We have found the registered reference resolver, we need to generate this
                        break;
                    }
                }
            }
        }

        return null;
    }

    private static void Execute(Compilation compilation, ImmutableArray<OutboundReferences> classRecords, SourceProductionContext context)
    {
        if (classRecords.IsDefaultOrEmpty)
        {
            Debug.WriteLine("No record groups found. Aborting");
            return;
        }
    }
        
    private static string GetNamespace(BaseTypeDeclarationSyntax syntax)
    {
        // Handle class nesting, move out until namespace
        var syntaxParent = syntax.Parent;
        while (syntaxParent != null
               && syntaxParent is not NamespaceDeclarationSyntax
               && syntaxParent is not FileScopedNamespaceDeclarationSyntax)
        {
            syntaxParent = syntaxParent.Parent;
        }

        // Build up namespace
        var @namespace = string.Empty;
        if (syntaxParent is BaseNamespaceDeclarationSyntax namespaceParent)
        {
            // We have a namespace. Use that as the type
            @namespace = namespaceParent.Name.ToString();
        
            while (true)
            {
                if (namespaceParent.Parent is not NamespaceDeclarationSyntax parent)
                {
                    break;
                }

                @namespace = $"{namespaceParent.Name}.{@namespace}";
                namespaceParent = parent;
            }
        }
        
        return @namespace;
    }
}