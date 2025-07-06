using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace MicroAPI
{
    [Generator]
    public class ControllerGenerator : IIncrementalGenerator
    {
        public const string FacadeSyntaxName = "HttpFacade";
        public const string AsyncSuffix = "Async";

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var facadeDeclarations = context.SyntaxProvider
                .CreateSyntaxProvider(
                    predicate: static (s, _) => IsFacadeSyntaxNode(s),
                    transform: static (ctx, _) => GetFacadeTypeSymbol(ctx))
                .Where(static m => m is not null)
                .Collect()
                .SelectMany((symbols, _) => symbols.Distinct(SymbolEqualityComparer.IncludeNullability))
                .Select((symbol, _) => symbol as INamedTypeSymbol);

            context.RegisterSourceOutput(facadeDeclarations,
                static (spc, facadeClass) =>
                {
                    if (facadeClass is not null)
                    {
                        ProcessFacadeClass(spc, facadeClass);
                    }
                });
        }

        private static bool IsFacadeSyntaxNode(SyntaxNode node)
        {
            if (node is not ClassDeclarationSyntax { AttributeLists.Count: > 0 } classDecl)
            {
                return false;
            }

            var hasFacadeAttribute = classDecl.AttributeLists
                    .SelectMany(al => al.Attributes)
                    .Any(a =>
                    {
                        var name = a.Name.ToString();
                        return name == nameof(HttpFacadeAttribute)
                               || name.StartsWith(nameof(HttpFacadeAttribute) + "<")
                               || name == FacadeSyntaxName
                               || name.StartsWith(FacadeSyntaxName + "<");
                    });

            // Check if it's a partial class
            var isPartial = classDecl.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword));
            return hasFacadeAttribute && isPartial;
        }

        private static INamedTypeSymbol? GetFacadeTypeSymbol(GeneratorSyntaxContext context)
        {
            var classDeclaration = (ClassDeclarationSyntax)context.Node;

            if (context.SemanticModel.GetDeclaredSymbol(classDeclaration) is { } symbol &&
                symbol.GetAttributes().Any(a =>
                    a.AttributeClass?.Name == nameof(HttpFacadeAttribute) ||
                    (a.AttributeClass?.Name.StartsWith(nameof(HttpFacadeAttribute)) == true &&
                     a.AttributeClass.TypeArguments.Length > 0)))
            {
                return symbol;
            }

            return null;
        }

        private static void ProcessFacadeClass(SourceProductionContext context, INamedTypeSymbol controllerClass)
        {
            var facadeAttribute = controllerClass.GetAttributes()
                .FirstOrDefault(a =>
                    a.AttributeClass?.Name == nameof(HttpFacadeAttribute) ||
                    (a.AttributeClass?.Name.StartsWith(nameof(HttpFacadeAttribute)) == true &&
                     a.AttributeClass.TypeArguments.Length > 0));

            if (facadeAttribute is null)
            {
                return;
            }

            var controllerName = controllerClass.Name.EndsWith("Controller") ?
                               controllerClass.Name.Substring(0, controllerClass.Name.Length - "Controller".Length) :
                               controllerClass.Name;

            var serviceType = GetServiceType(controllerClass, facadeAttribute);
            if (serviceType is null)
            {
                return;
            }

            // Generate the implementation part for the partial controller
            GeneratePartialControllerImplementation(context, controllerClass, serviceType, controllerName);
        }
        private static INamedTypeSymbol? GetServiceType(INamedTypeSymbol facadeClass, AttributeData facadeAttribute)
        {
            // Get service type from attribute or use the interface itself if it's an interface
            INamedTypeSymbol? serviceType = null;
            if (facadeClass.TypeKind == TypeKind.Interface)
            {
                serviceType = facadeClass;
            }
            else
            {
                // Check if it's a generic HttpFacadeAttribute<TService>
                if (facadeAttribute.AttributeClass?.TypeArguments.Length > 0)
                {
                    // Get the service type from the generic type argument
                    serviceType = (INamedTypeSymbol)facadeAttribute.AttributeClass.TypeArguments[0];
                }
                else
                {
                    // Try to get from attribute first (non-generic form)
                    foreach (var namedArgument in facadeAttribute.NamedArguments)
                    {
                        // ReSharper disable once InvertIf
                        if (namedArgument is { Key: nameof(HttpFacadeAttribute.Service), Value.Value: INamedTypeSymbol serviceSymbol })
                        {
                            serviceType = serviceSymbol;
                            break;
                        }
                    }

                    // If not specified in attribute, try to find from implemented interfaces
                    if (serviceType is null && facadeClass.Interfaces.Length > 0)
                    {
                        serviceType = facadeClass.Interfaces[0];
                    }
                }
            }

            return serviceType;
        }

        private static HashSet<string> ExtractRouteParameters(string routePath)
        {
            var parameters = new HashSet<string>();
            if (string.IsNullOrEmpty(routePath))
            {
                return parameters;
            }

            // Find all segments like {paramName} in the route
            // ReSharper disable once UseCollectionExpression
            var segments = routePath.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var segment in segments)
            {
                // ReSharper disable once InvertIf
                if (segment.StartsWith("{") && segment.EndsWith("}"))
                {
                    // Extract parameter name without braces
                    var paramName = segment.Substring(1, segment.Length - 2);

                    // Handle constraints like {id:int}
                    var colonIndex = paramName.IndexOf(':');
                    if (colonIndex > 0)
                    {
                        paramName = paramName.Substring(0, colonIndex);
                    }

                    parameters.Add(paramName);
                }
            }

            return parameters;
        }

        private static void GeneratePartialControllerImplementation(SourceProductionContext context, INamedTypeSymbol controllerClass,
            INamedTypeSymbol serviceType, string controllerName)
        {
            // Get methods from the service interface
            var methods = controllerClass.GetMembers().OfType<IMethodSymbol>()
                .Where(m => m.MethodKind == MethodKind.Ordinary).ToList();

            if (!methods.Any())
            {
                return;
            }

            var sourceBuilder = new StringBuilder();

            var controllerNs = controllerClass.ContainingNamespace.ToDisplayString();

            sourceBuilder.AppendLine("using Microsoft.AspNetCore.Mvc;");
            sourceBuilder.AppendLine("using System;");
            sourceBuilder.AppendLine("using System.Threading.Tasks;");
            sourceBuilder.AppendLine();
            sourceBuilder.AppendLine("#nullable enable");
            sourceBuilder.AppendLine("#pragma warning disable");
            sourceBuilder.AppendLine();
            sourceBuilder.AppendLine($"namespace {controllerNs}");
            sourceBuilder.AppendLine("{");
            sourceBuilder.AppendLine("    [ApiController]");
            sourceBuilder.AppendLine("    [Route(\"[controller]\")]");
            sourceBuilder.AppendLine($"    public partial class {controllerClass.Name} : ControllerBase");
            sourceBuilder.AppendLine("    {");
            sourceBuilder.AppendLine("        [Microsoft.AspNetCore.Components.Inject]");
            sourceBuilder.AppendLine($"        private {serviceType.ToDisplayString()} _service {{ get; set; }} = null!;");
            sourceBuilder.AppendLine();

            // Generate request DTOs inside the controller class

            foreach (var method in methods)
            {
                var httpMethodAttribute = method.GetAttributes()
                    .FirstOrDefault(a => a.AttributeClass?.BaseType?.Name == nameof(HttpMethodAttributeBase));

                if (httpMethodAttribute is null)
                {
                    continue;
                }

                var isGetMethod = httpMethodAttribute.AttributeClass?.Name == nameof(GetAttribute);
                var httpMethod = httpMethodAttribute.AttributeClass?.Name.Replace(nameof(Attribute), string.Empty);

                // Get custom method name if specified
                var methodName = method.Name;
                var methodNameWithoutAsync = methodName.EndsWith(AsyncSuffix) ? methodName.Substring(0, methodName.Length - AsyncSuffix.Length) : methodName;
                var routePath = methodNameWithoutAsync;

                var customRoutePath = GeneratorHelper.GetConstructorArgument(httpMethodAttribute, 0);

                if (customRoutePath is not null)
                {
                    routePath = customRoutePath;
                }

                foreach (var namedArgument in httpMethodAttribute.NamedArguments)
                {
                    if (namedArgument is { Key: nameof(HttpMethodAttributeBase.MethodName), Value.Value: string customMethodName }
                        && !string.IsNullOrEmpty(customMethodName))
                    {
                        methodName = customMethodName;
                    }
                }

                var returnType = method.ReturnType.ToDisplayString();
                var requestName = $"{methodNameWithoutAsync}Request";

                var routeParameters = ExtractRouteParameters(routePath);

                // Check for route parameters that don't exist in the method signature
                var methodParameterNames = method.Parameters.Select(p => p.Name).ToImmutableHashSet();
                WarnIfHasUnmatchedRouteParam(context, routeParameters, methodParameterNames, method, routePath);

                // Filter method parameters to exclude those that are route parameters
                var nonRouteParameters = method.Parameters
                    .Where(p => !routeParameters.Contains(p.Name))
                    .ToList();

                // Generate request DTO only if there are non-route parameters and not a GET method
                if (nonRouteParameters.Count > 0 && !isGetMethod)
                {
                    sourceBuilder.Append($"        public record {requestName}(");
                    sourceBuilder.Append(string.Join(", ", nonRouteParameters.Select(p => $"{p.Type.ToDisplayString()} {GeneratorHelper.ToPascalCase(p.Name)}")));
                    sourceBuilder.AppendLine(");");
                    sourceBuilder.AppendLine();
                }

                // Generate controller method
                sourceBuilder.AppendLine($"        [Http{httpMethod}(\"{routePath}\")]");

                // Copy other attributes from the original method (excluding HttpMethod attributes)
                foreach (var attribute in method.GetAttributes())
                {
                    // Skip HttpMethod attributes as we already added them
                    if (attribute.AttributeClass?.BaseType?.Name == nameof(HttpMethodAttributeBase))
                    {
                        continue;
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
                            attributeText.Append(')');
                        }
                    }

                    // Add named arguments
                    if (attribute.NamedArguments.Length > 0)
                    {
                        if (attribute.ConstructorArguments.Length == 0)
                        {
                            attributeText.Append('(');
                        }
                        else
                        {
                            attributeText.Append(", ");
                        }

                        attributeText.Append(string.Join(", ", attribute.NamedArguments
                            .Select(arg => $"{arg.Key} = {GeneratorHelper.FormatArgument(arg.Value)}")));

                        if (attribute.ConstructorArguments.Length == 0)
                        {
                            attributeText.Append(')');
                        }
                    }

                    attributeText.Append(']');
                    sourceBuilder.AppendLine(attributeText.ToString());
                }

                sourceBuilder.Append($"        public {returnType} {methodName}Facade(");

                // Build method parameters and service call arguments in original order
                var methodParameters = new List<string>();
                var serviceCallParameters = new List<string>();
                var hasRequestDto = nonRouteParameters.Count > 0 && !isGetMethod;

                if (isGetMethod)
                {
                    foreach (var param in method.Parameters)
                    {
                        methodParameters.Add(routeParameters.Contains(param.Name)
                            ? $"[FromRoute] {param.Type.ToDisplayString()} {param.Name}"
                            : $"[FromQuery] {param.Type.ToDisplayString()} {param.Name}");

                        // Service call arguments are the same as parameter names
                        serviceCallParameters.Add(param.Name);
                    }
                }
                else
                {
                    // First add route parameters
                    methodParameters.AddRange(
                        method.Parameters.Where(p => routeParameters.Contains(p.Name))
                            .Select(param => $"[FromRoute] {param.Type.ToDisplayString()} {param.Name}"));

                    // Then add the request DTO if needed
                    if (hasRequestDto)
                    {
                        methodParameters.Add($"[FromBody] {requestName} request");
                    }

                    // Build service call arguments in original order
                    serviceCallParameters.AddRange(
                        method.Parameters.Select(
                            param =>
                                routeParameters.Contains(param.Name)
                                ? param.Name
                                : $"request.{GeneratorHelper.ToPascalCase(param.Name)}"));
                }

                // Add parameters to method signature
                if (methodParameters.Count > 0)
                {
                    sourceBuilder.AppendLine(string.Join(", ", methodParameters) + ")");
                    sourceBuilder.AppendLine($"            => _service.{method.Name}({string.Join(", ", serviceCallParameters)});");
                }
                else
                {
                    sourceBuilder.AppendLine(")");
                    sourceBuilder.AppendLine($"            => _service.{method.Name}();");
                }

                sourceBuilder.AppendLine();
            }

            sourceBuilder.AppendLine("    }");
            sourceBuilder.AppendLine();

            sourceBuilder.AppendLine("}");

            context.AddSource($"{controllerName}Controller.g.cs", SourceText.From(sourceBuilder.ToString(), Encoding.UTF8));
        }

        private static void WarnIfHasUnmatchedRouteParam(SourceProductionContext context, HashSet<string> routeParameters,
            ImmutableHashSet<string> methodParameterNames, IMethodSymbol method, string routePath)
        {
            var unmatchedParams = routeParameters.Where(p => !methodParameterNames.Contains(p)).ToList();
            if (unmatchedParams.Any())
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    new DiagnosticDescriptor(
                        id: "MA003",
                        title: "Route parameter not found in method signature",
                        messageFormat: "Route parameter '{0}' in route '{1}' not found in method signature",
                        category: "MicroAPI",
                        defaultSeverity: DiagnosticSeverity.Warning,
                        isEnabledByDefault: true),
                    method.Locations.FirstOrDefault(),
                    unmatchedParams.First(), routePath));
            }
        }
    }
}