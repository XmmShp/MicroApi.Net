using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace MicroAPI;

[Generator]
public class ControllerGenerator : IIncrementalGenerator
{
    public const string FacadeSuffix = "Facade";
    public const string InterfacePrefix = "I";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var facadeDeclarations = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (s, _) => IsFacadeSyntaxNode(s),
                transform: static (ctx, _) => GetFacadeTypeSymbol(ctx))
            .Where(static m => m is not null)!
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
        return node is MemberDeclarationSyntax { AttributeLists.Count: > 0 }
            and (ClassDeclarationSyntax or InterfaceDeclarationSyntax);
    }

    private static INamedTypeSymbol? GetFacadeTypeSymbol(GeneratorSyntaxContext context)
    {
        var memberDeclaration = (MemberDeclarationSyntax)context.Node;

        if (context.SemanticModel.GetDeclaredSymbol(memberDeclaration) is INamedTypeSymbol symbol &&
            symbol.GetAttributes().Any(a => a.AttributeClass?.Name == nameof(HttpFacadeAttribute)))
        {
            return symbol;
        }

        return null;
    }

    private static void ProcessFacadeClass(SourceProductionContext context, INamedTypeSymbol facadeClass)
    {
        var facadeAttribute = facadeClass.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.Name == nameof(HttpFacadeAttribute));

        if (facadeAttribute is null)
        {
            return;
        }

        var controllerName = GetControllerName(facadeClass, facadeAttribute);
        var serviceType = GetServiceType(facadeClass, facadeAttribute);
        if (serviceType is null)
        {
            return;
        }

        var (controllerNamespace, dtoNamespace) = GetNamespaces(facadeAttribute);

        GenerateController(context, facadeClass, serviceType, controllerName!, controllerNamespace, dtoNamespace);
    }

    private static (string? controllerNamespace, string? dtoNamespace) GetNamespaces(AttributeData facadeAttribute)
    {
        string? controllerNamespace = null;
        string? dtoNamespace = null;

        foreach (var namedArgument in facadeAttribute.NamedArguments)
        {
            switch (namedArgument)
            {
                case { Key: nameof(HttpFacadeAttribute.ControllerNamespace), Value.Value: string controllerNs }:
                    controllerNamespace = controllerNs;
                    break;
                case { Key: nameof(HttpFacadeAttribute.DtoNamespace), Value.Value: string dtoNs }:
                    dtoNamespace = dtoNs;
                    break;
            }
        }

        return (controllerNamespace, dtoNamespace);
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
            // Try to get from attribute first
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

        return serviceType;
    }

    private static string? GetControllerName(INamedTypeSymbol facadeClass, AttributeData facadeAttribute)
    {
        var controllerName = GetConstructorArgument(facadeAttribute, 0);
        if (string.IsNullOrEmpty(controllerName))
        {
            controllerName = facadeClass.TypeKind switch
            {
                TypeKind.Class => facadeClass.Name.EndsWith(FacadeSuffix)
                    ? facadeClass.Name.Substring(0, facadeClass.Name.Length - FacadeSuffix.Length)
                    : facadeClass.Name,
                TypeKind.Interface => facadeClass.Name.StartsWith(InterfacePrefix)
                    ? facadeClass.Name.Substring(InterfacePrefix.Length,
                        facadeClass.Name.Length - InterfacePrefix.Length)
                    : facadeClass.Name,
                _ => facadeClass.Name
            };
        }

        return controllerName;
    }

    private static string? GetConstructorArgument(AttributeData attribute, int index)
        => attribute.ConstructorArguments.Length > index
            ? attribute.ConstructorArguments[index].Value?.ToString()
            : null;

    private static HashSet<string> ExtractRouteParameters(string routePath)
    {
        var parameters = new HashSet<string>();
        if (string.IsNullOrEmpty(routePath))
        {
            return parameters;
        }

        // Find all segments like {paramName} in the route
        var segments = routePath.Split(['/'], StringSplitOptions.RemoveEmptyEntries);
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

    private static void GenerateController(SourceProductionContext context, INamedTypeSymbol facadeClass,
        INamedTypeSymbol serviceType, string controllerName, string? controllerNamespace = null, string? dtoNamespace = null)
    {
        // Get methods from the facade class or interface
        var methods = facadeClass.GetMembers().OfType<IMethodSymbol>()
            .Where(m => m.MethodKind == MethodKind.Ordinary).ToList();

        if (!methods.Any())
        {
            return;
        }

        var sourceBuilder = new StringBuilder();

        var baseNs = facadeClass.ContainingNamespace.ToDisplayString();
        var controllerNs = controllerNamespace ?? $"{baseNs}.Controllers";
        var dtoNs = dtoNamespace ?? controllerNs;

        var requestDtos = new List<string>();

        sourceBuilder.AppendLine("using Microsoft.AspNetCore.Mvc;");
        sourceBuilder.AppendLine("using System;");
        sourceBuilder.AppendLine("using System.Threading.Tasks;");
        if (controllerNs != dtoNs)
        {
            sourceBuilder.AppendLine($"using {dtoNs};");
        }
        sourceBuilder.AppendLine();
        sourceBuilder.AppendLine($"namespace {controllerNs}");
        sourceBuilder.AppendLine("{");
        sourceBuilder.AppendLine("    [ApiController]");
        sourceBuilder.AppendLine("    [Route(\"[controller]\")]");
        sourceBuilder.AppendLine($"    public class {controllerName}Controller : ControllerBase");
        sourceBuilder.AppendLine("    {");
        sourceBuilder.AppendLine($"        private readonly {serviceType.ToDisplayString()} _service;");
        sourceBuilder.AppendLine();
        sourceBuilder.AppendLine($"        public {controllerName}Controller({serviceType.ToDisplayString()} service)");
        sourceBuilder.AppendLine("        {");
        sourceBuilder.AppendLine("            _service = service;");
        sourceBuilder.AppendLine("        }");
        sourceBuilder.AppendLine();

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
            var routePath = methodName;

            var customRoutePath = GetConstructorArgument(httpMethodAttribute, 0);

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
            var requestDtoName = $"{methodName}Request";

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
                var requestDtoBuilder = new StringBuilder();

                requestDtoBuilder.Append($"public record {requestDtoName}(");
                requestDtoBuilder.Append(string.Join(", ", nonRouteParameters.Select(p => $"{p.Type.ToDisplayString()} {p.Name}")));
                requestDtoBuilder.AppendLine(");");
                requestDtos.Add(requestDtoBuilder.ToString());
            }

            // Generate controller method
            sourceBuilder.AppendLine($"        [Http{httpMethod}(\"{routePath}\")]");

            sourceBuilder.Append($"        public {returnType} {methodName}(");

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
                    methodParameters.Add($"[FromBody] {requestDtoName} request");
                }

                // Build service call arguments in original order
                serviceCallParameters.AddRange(
                    method.Parameters.Select(
                        param =>
                            routeParameters.Contains(param.Name)
                            ? param.Name
                            : $"request.{param.Name}"));
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

        // Add request DTOs
        // ReSharper disable once InvertIf
        if (requestDtos.Any())
        {
            var dtosBuilder = new StringBuilder();

            dtosBuilder.AppendLine("using System;");
            dtosBuilder.AppendLine();
            dtosBuilder.AppendLine($"namespace {dtoNs}");
            dtosBuilder.AppendLine("{");

            foreach (var dto in requestDtos)
            {
                dtosBuilder.AppendLine($"    {dto}");
            }

            dtosBuilder.AppendLine("}");

            context.AddSource($"{controllerName}Dtos.g.cs", SourceText.From(dtosBuilder.ToString(), Encoding.UTF8));
        }
    }

    private static void WarnIfHasUnmatchedRouteParam(SourceProductionContext context, HashSet<string> routeParameters,
        ImmutableHashSet<string> methodParameterNames, IMethodSymbol method, string routePath)
    {
        var unmatchedRouteParams = routeParameters.Where(rp => !methodParameterNames.Contains(rp)).ToList();

        if (unmatchedRouteParams.Any())
        {
            context.ReportDiagnostic(Diagnostic.Create(
                new DiagnosticDescriptor(
                    id: "CG001",
                    title: "Unmatched route parameter",
                    messageFormat: "Route parameter '{0}' in route '{1}' does not match any parameter in method '{2}'",
                    category: "ControllerGenerator",
                    defaultSeverity: DiagnosticSeverity.Warning,
                    isEnabledByDefault: true),
                method.Locations.FirstOrDefault(),
                string.Join(", ", unmatchedRouteParams),
                routePath,
                method.Name));
        }
    }
}