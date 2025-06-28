using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MicroAPI
{
    [Generator]
    public class DtoGenerator : IIncrementalGenerator
    {
        private const string DtoAttributeSyntaxName = "Dto";
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            // Create diagnostic descriptor for non-partial Dto classes
            var nonPartialDtoDiagnostic = new DiagnosticDescriptor(
                id: "MA002",
                title: "Dto class must be partial",
                messageFormat: "Class '{0}' with [Dto] attribute must be declared as partial",
                category: "MicroAPI",
                defaultSeverity: DiagnosticSeverity.Error,
                isEnabledByDefault: true);

            // Handle non-partial Dto classes for diagnostics
            var nonPartialDtoClasses = context.SyntaxProvider
                .CreateSyntaxProvider(
                    predicate: static (node, _) => IsDtoClass(node),
                    transform: static (ctx, _) =>
                    {
                        var classDecl = (ClassDeclarationSyntax)ctx.Node;
                        return classDecl.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword))
                            ? default
                            : (classDecl, ctx.SemanticModel.GetDeclaredSymbol(classDecl));
                    })
                .Where(static info => info.classDecl != null)
                .Select((info, _) => info);

            // Register diagnostic output for non-partial Dto classes
            context.RegisterSourceOutput(nonPartialDtoClasses,
                (spc, info) =>
                {
                    spc.ReportDiagnostic(Diagnostic.Create(
                        nonPartialDtoDiagnostic,
                        info.classDecl.Identifier.GetLocation(),
                        info.Item2?.Name ?? info.classDecl.Identifier.ValueText));
                });

            // Handle Dto classes for code generation
            var dtoClasses = context.SyntaxProvider
                .CreateSyntaxProvider(
                    predicate: static (node, _) => IsDtoClass(node),
                    transform: static (ctx, _) => GetDtoClassInfo(ctx))
                .Where(static info => info is not null)
                .Collect()
                .SelectMany((infos, _) => infos.Distinct())
                .Select((info, _) => info);

            context.RegisterSourceOutput(dtoClasses,
                static (spc, dtoInfo) =>
                {
                    if (dtoInfo is not null)
                    {
                        GenerateDto(spc, dtoInfo.Value);
                    }
                });
        }

        private static bool IsDtoClass(SyntaxNode node)
        {
            if (node is not ClassDeclarationSyntax { AttributeLists.Count: > 0 } classDecl)
            {
                return false;
            }

            var hasDtoAttribute = classDecl.AttributeLists
                .SelectMany(al => al.Attributes)
                .Any(a =>
                {
                    var name = a.Name.ToString();
                    return name == nameof(DtoAttribute)
                           || name.StartsWith(nameof(DtoAttribute) + "<")
                           || name == DtoAttributeSyntaxName
                           || name.StartsWith(DtoAttributeSyntaxName + "<");
                });

            // We still want to return true for non-partial classes so we can report the diagnostic
            return hasDtoAttribute;
        }

        private static (ClassDeclarationSyntax, INamedTypeSymbol, INamedTypeSymbol, string[])? GetDtoClassInfo(GeneratorSyntaxContext context)
        {
            var classDecl = (ClassDeclarationSyntax)context.Node;
            var model = context.SemanticModel;
            var typeSymbol = model.GetDeclaredSymbol(classDecl);

            // Check for both non-generic and generic DtoAttribute
            var dtoAttribute = typeSymbol?.GetAttributes()
                .FirstOrDefault(a => a.AttributeClass?.Name == nameof(DtoAttribute) ||
                                     (a.AttributeClass?.Name.StartsWith(nameof(DtoAttribute)) == true &&
                                      a.AttributeClass.TypeArguments.Length > 0));

            if (dtoAttribute == null)
            {
                return null;
            }

            // Skip non-partial classes as they will be handled by the diagnostic system
            if (!classDecl.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)))
            {
                return null;
            }

            INamedTypeSymbol entityType;

            // Handle both generic and non-generic DTOAttribute
            if (dtoAttribute.AttributeClass!.TypeArguments.Length > 0)
            {
                // Generic DTOAttribute<TEntity>
                entityType = (INamedTypeSymbol)dtoAttribute.AttributeClass.TypeArguments[0];
            }
            else
            {
                // Non-generic DTOAttribute(Type entityType)
                entityType = (INamedTypeSymbol)dtoAttribute.ConstructorArguments[0].Value!;
            }

            // Get ignored properties
            var ignoredProperties = Array.Empty<string>();

            // Check for IgnoredProperties in named arguments
            foreach (var namedArg in dtoAttribute.NamedArguments)
            {
                // ReSharper disable once InvertIf
                if (namedArg is { Key: nameof(DtoAttribute.IgnoredProperties), Value.Values.Length: > 0 })
                {
                    ignoredProperties = namedArg.Value.Values
                        .Select(v => v.Value?.ToString() ?? string.Empty)
                        .Where(s => !string.IsNullOrEmpty(s))
                        .ToArray();
                    break;
                }
            }

            return (classDecl, typeSymbol!, entityType, ignoredProperties);
        }

        private static void GenerateDto(SourceProductionContext context,
             (ClassDeclarationSyntax, INamedTypeSymbol, INamedTypeSymbol, string[]) dtoInfo)
        {
            var (_, dtoType, entityType, ignoredProperties) = dtoInfo;
            var sourceBuilder = new StringBuilder();
            dtoType.ContainingNamespace.ToDisplayString();

            // Generate the DTO class
            GenerateDtoClassDefinition(sourceBuilder, dtoType, entityType, ignoredProperties);

            context.AddSource($"{dtoType.Name}.g.cs", SourceText.From(sourceBuilder.ToString(), Encoding.UTF8));
        }

        private static void GenerateDtoClassDefinition(StringBuilder sourceBuilder, INamedTypeSymbol dtoType,
            INamedTypeSymbol entityType, string[] ignoredProperties)
        {
            var namespaceName = dtoType.ContainingNamespace.ToDisplayString();

            sourceBuilder.AppendLine("using System;");
            sourceBuilder.AppendLine("using System.Linq;");
            sourceBuilder.AppendLine();
            sourceBuilder.AppendLine("#nullable enable");
            sourceBuilder.AppendLine("#pragma warning disable");
            sourceBuilder.AppendLine();
            sourceBuilder.AppendLine($"namespace {namespaceName}");
            sourceBuilder.AppendLine("{");
            sourceBuilder.AppendLine($"    public partial class {dtoType.Name}");
            sourceBuilder.AppendLine("    {");

            // Get existing properties defined in the partial class
            var existingProperties = new HashSet<string>(dtoType.GetMembers()
                .OfType<IPropertySymbol>()
                .Select(p => p.Name));

            // Generate properties from entity, excluding ignored ones and already defined ones
            foreach (var member in entityType.GetMembers().OfType<IPropertySymbol>())
            {
                // Skip if property is ignored or already defined in the partial class
                if (ignoredProperties.Contains(member.Name) || existingProperties.Contains(member.Name))
                {
                    continue;
                }

                var propertyType = GetPropertyType(member.Type);
                sourceBuilder.AppendLine($"        public {propertyType} {member.Name} {{ get; set; }}");
            }

            sourceBuilder.AppendLine("    }");
            sourceBuilder.AppendLine("}");
        }

        private static string GetPropertyType(ITypeSymbol typeSymbol)
        {
            // Handle collection types (List<T>, IEnumerable<T>, etc.)
            // ReSharper disable once InvertIf
            if (typeSymbol is INamedTypeSymbol { IsGenericType: true } namedType)
            {
                var typeArgs = namedType.TypeArguments;
                // ReSharper disable once InvertIf
                if (typeArgs.Length == 1)
                {
                    var elementType = typeArgs[0];
                    var collectionTypeName = namedType.ConstructedFrom.ToDisplayString();

                    // For generic collections, process the element type
                    var elementTypeName = GetPropertyType(elementType);
                    return $"{collectionTypeName.Split('<')[0]}<{elementTypeName}>";
                }
            }

            // Return the original type for all types
            return typeSymbol.ToDisplayString();
        }
    }
}
