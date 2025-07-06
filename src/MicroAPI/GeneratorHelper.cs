using Microsoft.CodeAnalysis;
using System;
using System.Linq;

namespace MicroAPI;

public static class GeneratorHelper
{
    /// <summary>
    /// Formats a TypedConstant into a string representation.
    /// </summary>
    /// <param name="arg">The TypedConstant to format.</param>
    /// <returns>A string representation of the TypedConstant.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the TypedConstant kind is not supported.</exception>
    public static string FormatArgument(TypedConstant arg)
    {
        if (arg.IsNull)
        {
            return "null";
        }

        switch (arg.Kind)
        {
            // Don't add empty array initializer if there are no values
            case TypedConstantKind.Array when arg.Values.Length == 0:
                // Skip empty arrays completely to avoid compilation errors
                return string.Empty;
            case TypedConstantKind.Array:
                return $"new[] {{{string.Join(", ", arg.Values.Select(FormatArgument))}}}";
            case TypedConstantKind.Type:
                {
                    // Use fully qualified name for type arguments
                    var typeSymbol = arg.Value as ITypeSymbol;
                    return $"typeof({typeSymbol?.ToDisplayString() ?? arg.Value})";
                }
            case TypedConstantKind.Error:
            case TypedConstantKind.Primitive:
            case TypedConstantKind.Enum:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        return arg.Value switch
        {
            string stringValue => $"\"{stringValue.Replace("\"", "\\\"")}\"",
            bool boolValue => boolValue ? "true" : "false",
            char charValue => $"'{charValue}'",
            _ => arg.Value?.ToString() ?? "null"
        };
    }

    /// <summary>
    /// Gets the type name of the given type symbol, including nullable annotations and generic type arguments.
    /// </summary>
    /// <param name="typeSymbol">The type symbol to get the name for.</param>
    /// <returns>The type name as a string.</returns>
    public static string GetTypeName(ITypeSymbol typeSymbol)
    {
        // Check if the type is nullable and preserve that information
        var isNullable = typeSymbol.NullableAnnotation == NullableAnnotation.Annotated;

        // Handle generic types (including collections like List<T>, Dictionary<K,V>, etc.)
        if (typeSymbol is INamedTypeSymbol { IsGenericType: true } namedType)
        {
            var typeArgs = namedType.TypeArguments;
            if (typeArgs.Length >= 1)
            {
                // Get the base generic type name
                var genericTypeName = namedType.ConstructedFrom.ToDisplayString().Split('<')[0];

                // Process all generic type arguments
                var processedTypeArgs = new string[typeArgs.Length];
                for (var i = 0; i < typeArgs.Length; i++)
                {
                    processedTypeArgs[i] = GetTypeName(typeArgs[i]);
                }

                // Combine the generic type with its arguments
                var result = $"{genericTypeName}<{string.Join(", ", processedTypeArgs)}>";

                // Add nullable annotation if needed
                return isNullable ? $"{result}?" : result;
            }
        }

        // Return the fully qualified type with nullable annotation if needed
        var baseType = typeSymbol.ToDisplayString();
        return isNullable && !baseType.EndsWith("?") ? $"{baseType}?" : baseType;
    }

    /// <summary>
    /// Converts a string to PascalCase (first letter uppercase, rest preserved)
    /// </summary>
    public static string ToPascalCase(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return input;
        }

        return char.ToUpperInvariant(input[0]) + input.Substring(1);
    }

    /// <summary>
    /// Gets the constructor argument value from an attribute data.
    /// </summary>
    /// <param name="attribute">The attribute data to get the constructor argument from.</param>
    /// <param name="index">The index of the constructor argument to get.</param>
    /// <returns>The constructor argument value as a string, or null if the index is out of range.</returns>
    public static string? GetConstructorArgument(AttributeData attribute, int index)
        => attribute.ConstructorArguments.Length > index
            ? attribute.ConstructorArguments[index].Value?.ToString()
            : null;
}
