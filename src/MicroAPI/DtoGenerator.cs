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

        private static (ClassDeclarationSyntax, INamedTypeSymbol, INamedTypeSymbol, string[], INamedTypeSymbol[])? GetDtoClassInfo(GeneratorSyntaxContext context)
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
            var ignoredAttributes = Array.Empty<INamedTypeSymbol>();

            // Check for named arguments
            foreach (var namedArg in dtoAttribute.NamedArguments)
            {
                // Get ignored properties
                if (namedArg is { Key: nameof(DtoAttribute.IgnoredProperties), Value.Values.Length: > 0 })
                {
                    ignoredProperties = namedArg.Value.Values
                        .Select(v => v.Value?.ToString() ?? string.Empty)
                        .Where(s => !string.IsNullOrEmpty(s))
                        .ToArray();
                }

                // Get ignored attributes
                if (namedArg is { Key: nameof(DtoAttribute.IgnoredAttributes), Value.Values.Length: > 0 })
                {
                    ignoredAttributes = namedArg.Value.Values
                        .Select(v => v.Value as INamedTypeSymbol)
                        .Where(s => s != null)
                        .ToArray()!;
                }
            }

            return (classDecl, typeSymbol!, entityType, ignoredProperties, ignoredAttributes);
        }

        private static void GenerateDto(SourceProductionContext context,
             (ClassDeclarationSyntax, INamedTypeSymbol, INamedTypeSymbol, string[], INamedTypeSymbol[]) dtoInfo)
        {
            var (_, dtoType, entityType, ignoredProperties, ignoredAttributes) = dtoInfo;
            var sourceBuilder = new StringBuilder();
            dtoType.ContainingNamespace.ToDisplayString();

            // Generate the DTO class
            GenerateDtoClassDefinition(sourceBuilder, dtoType, entityType, ignoredProperties, ignoredAttributes);

            context.AddSource($"{dtoType.Name}.g.cs", SourceText.From(sourceBuilder.ToString(), Encoding.UTF8));
        }

        private static void GenerateDtoClassDefinition(StringBuilder sourceBuilder, INamedTypeSymbol dtoType,
            INamedTypeSymbol entityType, string[] ignoredProperties, INamedTypeSymbol[] ignoredAttributes)
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

            // Check if the entity type is a record
            bool isRecord = entityType.IsRecord;

            // Generate properties from entity, excluding ignored ones and already defined ones
            foreach (var member in entityType.GetMembers().OfType<IPropertySymbol>())
            {
                // Skip if property is ignored or already defined in the partial class
                if (ignoredProperties.Contains(member.Name) || existingProperties.Contains(member.Name))
                {
                    continue;
                }

                // Skip EqualityContract property for record types
                if (isRecord && member.Name == "EqualityContract")
                {
                    continue;
                }

                // Skip if property has any of the ignored attributes
                if (ignoredAttributes.Length > 0 && HasIgnoredAttribute(member, ignoredAttributes))
                {
                    continue;
                }

                var propertyType = GeneratorHelper.GetTypeName(member.Type);

                // Get attributes from the source property
                var attributes = member.GetAttributes();

                // Copy attributes from the source property to the generated property
                foreach (var attribute in attributes)
                {
                    // Skip attributes that are in the ignored list
                    if (ignoredAttributes.Length > 0 &&
                        attribute.AttributeClass != null &&
                        ignoredAttributes.Any(ignored => IsAttributeOfTypeOrDerivedFrom(attribute.AttributeClass, ignored)))
                    {
                        continue;
                    }

                    // Skip compiler-generated attributes
                    if (attribute.AttributeClass != null)
                    {
                        var attributeFullName = attribute.AttributeClass.ToDisplayString();
                        if (attributeFullName.Contains("System.Runtime.CompilerServices"))
                        {
                            continue;
                        }
                    }

                    // Generate the attribute with its parameters using fully qualified name
                    var attributeClass = attribute.AttributeClass;
                    if (attributeClass == null)
                        continue;

                    // Get fully qualified name
                    var fullyQualifiedName = attributeClass.ToDisplayString();

                    // Remove the 'Attribute' suffix if present in the display name
                    var attributeName = fullyQualifiedName.EndsWith("Attribute")
                        ? fullyQualifiedName.Substring(0, fullyQualifiedName.Length - "Attribute".Length)
                        : fullyQualifiedName;

                    var attributeText = new StringBuilder($"        [{attributeName}");

                    var needRightBracket = false;
                    // Add constructor arguments
                    if (attribute.ConstructorArguments.Length > 0)
                    {
                        var formattedArgs = attribute.ConstructorArguments
                            .Select(GeneratorHelper.FormatArgument)
                            .Where(arg => !string.IsNullOrEmpty(arg))
                            .ToList();

                        // Only add parentheses if there are non-empty arguments
                        if (formattedArgs.Any())
                        {
                            attributeText.Append('(');
                            attributeText.Append(string.Join(", ", formattedArgs));
                            needRightBracket = true;
                        }
                    }

                    // Add named arguments
                    if (attribute.NamedArguments.Length > 0)
                    {
                        if (attribute.ConstructorArguments.Length == 0
                            || attribute.ConstructorArguments
                                .Select(GeneratorHelper.FormatArgument)
                                .All(string.IsNullOrEmpty))
                        {
                            attributeText.Append('(');
                            needRightBracket = true;
                        }
                        else
                        {
                            attributeText.Append(", ");
                        }

                        attributeText.Append(string.Join(", ", attribute.NamedArguments
                            .Select(arg => $"{arg.Key} = {GeneratorHelper.FormatArgument(arg.Value)}")));
                    }

                    if (needRightBracket)
                    {
                        attributeText.Append(')');
                    }

                    attributeText.Append(']');
                    sourceBuilder.AppendLine(attributeText.ToString());
                }

                // Check if the property has a default value
                var defaultValueText = string.Empty;
                if (member.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax()
                    is PropertyDeclarationSyntax { Initializer: not null } propertySyntax)
                {
                    defaultValueText = $" = {propertySyntax.Initializer.Value};";
                }

                // Generate the property with required modifier and default value if applicable
                sourceBuilder.AppendLine($"        public {propertyType} {member.Name} {{ get; set; }}{defaultValueText}");
            }

            sourceBuilder.AppendLine("    }");
            sourceBuilder.AppendLine("}");
        }

        private static bool HasIgnoredAttribute(IPropertySymbol property, INamedTypeSymbol[] ignoredAttributes)
        {
            // Get all attributes on the property
            var propertyAttributes = property.GetAttributes();

            // Check if any property attribute is of an ignored type or derives from an ignored type
            return propertyAttributes.Select(propertyAttribute => propertyAttribute.AttributeClass)
                .OfType<INamedTypeSymbol>()
                .Any(attributeType =>
                    ignoredAttributes.Any(ignoredAttribute
                        => IsAttributeOfTypeOrDerivedFrom(attributeType, ignoredAttribute)));
        }

        private static bool IsAttributeOfTypeOrDerivedFrom(INamedTypeSymbol attributeType, INamedTypeSymbol baseType)
        {
            // Check if the attribute is the same type as the base type
            if (SymbolEqualityComparer.Default.Equals(attributeType, baseType))
            {
                return true;
            }

            // Check if the attribute derives from the base type
            var currentType = attributeType.BaseType;
            while (currentType != null)
            {
                if (SymbolEqualityComparer.Default.Equals(currentType, baseType))
                {
                    return true;
                }
                currentType = currentType.BaseType;
            }

            return false;
        }
    }
}
