using Microsoft.CodeAnalysis;
using System;
using System.Linq;

namespace MicroAPI;

internal class GeneratorHelper
{
    public static string FormatAttributeArgument(TypedConstant arg)
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
                return $"new[] {{{string.Join(", ", arg.Values.Select(FormatAttributeArgument))}}}";
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
}
