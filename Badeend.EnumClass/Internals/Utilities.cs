using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Badeend.EnumClass.Internals;

internal static class Utilities
{
	internal static INamedTypeSymbol? GetEnumClassType(this SemanticModel semanticModel, ExpressionSyntax expression)
	{
		var expressionTypeInfo = semanticModel.GetTypeInfo(expression);
		if (expressionTypeInfo.ConvertedType is not INamedTypeSymbol expressionType)
		{
			return null;
		}

		if (!expressionType.HasEnumClassAttribute())
		{
			return null;
		}

		return expressionType;
	}

	internal static bool HasEnumClassAttribute(this INamedTypeSymbol typeSymbol)
	{
		if (typeSymbol.TypeKind != TypeKind.Class)
		{
			// `Badeend.EnumClassAttribute` has `AttributeTargets.Class`, so we
			// can bail fast if the symbol is not a class. If the attribute is
			// applied to a non-class, the C# compiler will handle that for us.
			return false;
		}

		return typeSymbol.GetAttributes().Any(IsEnumClassAttribute);
	}

	private static bool IsEnumClassAttribute(AttributeData attribute)
	{
		var attributeClass = attribute.AttributeClass;
		if (attributeClass is null)
		{
			return false;
		}

		return attributeClass.Name == "EnumClassAttribute" && attributeClass.ContainingNamespace.ToString() == "Badeend";
	}

	internal static IEnumerable<INamedTypeSymbol> GetDirectEnumCases(this INamedTypeSymbol enumClass)
	{
		var expectedBaseType = enumClass.OriginalDefinition;

		return enumClass.GetTypeMembers()
			.Where(t => SymbolEqualityComparer.Default.Equals(t.BaseType?.OriginalDefinition, expectedBaseType));
	}

	internal static IEnumerable<INamedTypeSymbol> GetAllEnumCases(this INamedTypeSymbol enumClass)
	{
		return enumClass.GetDirectEnumCases().SelectMany(c => c.HasEnumClassAttribute() ? c.GetAllEnumCases() : [c]);
	}

	// Adapted from: https://github.com/dotnet/runtime/blob/0588f245dba6418349c22894b40337afc34f1cfc/src/libraries/System.Text.Json/Common/JsonCamelCaseNamingPolicy.cs
	internal static string ToCamelCase(this string input)
	{
		if (string.IsNullOrEmpty(input) || !char.IsUpper(input[0]))
		{
			return input;
		}

		char[] chars = input.ToCharArray();

		for (int i = 0; i < chars.Length; i++)
		{
			if (i == 1 && !char.IsUpper(chars[i]))
			{
				break;
			}

			bool hasNext = i + 1 < chars.Length;

			// Stop when next char is already lowercase.
			if (i > 0 && hasNext && !char.IsUpper(chars[i + 1]))
			{
				// If the next char is a space, lowercase current char before exiting.
				if (chars[i + 1] == ' ')
				{
					chars[i] = char.ToLowerInvariant(chars[i]);
				}

				break;
			}

			chars[i] = char.ToLowerInvariant(chars[i]);
		}

		return new string(chars);
	}
}
