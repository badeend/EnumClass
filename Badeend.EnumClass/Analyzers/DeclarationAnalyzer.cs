using System.Collections.Immutable;
using Badeend.EnumClass.Internals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Badeend.EnumClass.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DeclarationAnalyzer : DiagnosticAnalyzer
{
	public override sealed ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create([
		Diagnostics.EC1000_EnumClassMustBeAbstract,
		Diagnostics.EC1001_EnumCaseOutsideEnumClass,
		Diagnostics.EC1002_EnumCaseVisibility,
		Diagnostics.EC1003_MissingParameterlessPrivateConstructor,
		Diagnostics.EC1004_PrimaryConstructorsNotAllowed,
		Diagnostics.EC1005_PublicConstructorsNotAllowed,
		Diagnostics.EC1006_InvalidCasePlacement,
		Diagnostics.EC1007_CaseTypeParameters,
		Diagnostics.EC1008_BaseTypeSpecialization,
		Diagnostics.EC1030_UnrelatedNestedType,
		Diagnostics.EC1031_NoCases,
	]);

	public override sealed void Initialize(AnalysisContext context)
	{
		context.EnableConcurrentExecution(); // WARNING! Be careful not to store any state in the analyzer instances
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

		context.RegisterSymbolAction(this.AnalyzeNamedType, SymbolKind.NamedType);
	}

	private void AnalyzeNamedType(SymbolAnalysisContext context)
	{
		var type = (INamedTypeSymbol)context.Symbol;

		if (type.HasEnumClassAttribute())
		{
			this.ValidateEnumClass(context, type);
		}

		var enumClass = GetBaseTypeAsEnumClass(type);
		if (enumClass is not null)
		{
			this.ValidateEnumCase(context, enumClass, type);
		}
		else
		{
			this.ValidateNestedClass(context, type);
		}
	}

	private void ValidateEnumClass(SymbolAnalysisContext context, INamedTypeSymbol enumClass)
	{
		if (!enumClass.IsAbstract)
		{
			ReportDiagnostic(context, enumClass, Diagnostics.EC1000_EnumClassMustBeAbstract);
		}

		foreach (var constructor in enumClass.InstanceConstructors)
		{
			this.ValidatePublicConstructor(context, enumClass, constructor);
		}

		var hasCases = enumClass.GetTypeMembers().Any(c => c.BaseType is { } baseType && AreOfTheSameGenericType(enumClass, baseType));
		if (!hasCases)
		{
			ReportDiagnostic(context, enumClass, Diagnostics.EC1031_NoCases);
		}
	}

	private void ValidatePublicConstructor(SymbolAnalysisContext context, INamedTypeSymbol enumClass, IMethodSymbol constructor)
	{
		if (constructor.DeclaredAccessibility > Accessibility.Private)
		{
			switch (GetConstructorKind(constructor))
			{
				case ConstructorKind.ImplicitCopy:
					// There's no way for the developer to prevent this. So we'll (have to) allow it...
					return;

				case ConstructorKind.ImplicitParameterless:
					ReportDiagnostic(context, enumClass.Locations, Diagnostics.EC1003_MissingParameterlessPrivateConstructor);
					break;

				case ConstructorKind.Primary:
					ReportDiagnostic(context, GetPrimaryConstructorLocationsToReport(enumClass), Diagnostics.EC1004_PrimaryConstructorsNotAllowed);
					break;

				case ConstructorKind.Regular:
					ReportDiagnostic(context, constructor.Locations, Diagnostics.EC1005_PublicConstructorsNotAllowed);
					break;

				default:
					throw new Exception("Unknown ConstructorKind");
			}
		}
	}

	private void ValidateEnumCase(SymbolAnalysisContext context, INamedTypeSymbol enumClass, INamedTypeSymbol enumCase)
	{
		if (enumCase.ContainingType is null || SymbolEqualityComparer.Default.Equals(enumClass, enumCase.ContainingType) == false)
		{
			if (enumCase.ContainingType is not null && AreOfTheSameGenericType(enumClass, enumCase.ContainingType))
			{
				ReportDiagnostic(context, GetBaseClassLocationsToReport(enumCase), Diagnostics.EC1008_BaseTypeSpecialization, GetFormattedTypeName(enumCase.ContainingType.OriginalDefinition), GetFormattedTypeName(enumClass));
			}
			else if (IsDeeplyNestedInside(enumClass, enumCase))
			{
				ReportDiagnostic(context, enumCase, Diagnostics.EC1006_InvalidCasePlacement);
			}
			else
			{
				ReportDiagnostic(context, enumCase, Diagnostics.EC1001_EnumCaseOutsideEnumClass);
			}
		}

		if (enumCase.DeclaredAccessibility <= Accessibility.Private || enumCase.DeclaredAccessibility < enumClass.DeclaredAccessibility)
		{
			ReportDiagnostic(context, enumCase, Diagnostics.EC1002_EnumCaseVisibility);
		}

		if (enumCase.TypeParameters.IsEmpty == false)
		{
			ReportDiagnostic(context, GetTypeParameterLocationsToReport(enumCase), Diagnostics.EC1007_CaseTypeParameters);
		}
	}

	private static readonly SymbolDisplayFormat BaseClassDisplayFormat = new(
		globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
		typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameOnly,
		genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
		memberOptions: SymbolDisplayMemberOptions.None,
		delegateStyle: SymbolDisplayDelegateStyle.NameOnly,
		extensionMethodStyle: SymbolDisplayExtensionMethodStyle.Default,
		parameterOptions: SymbolDisplayParameterOptions.None,
		propertyStyle: SymbolDisplayPropertyStyle.NameOnly,
		localOptions: SymbolDisplayLocalOptions.None,
		kindOptions: SymbolDisplayKindOptions.None,
		miscellaneousOptions: SymbolDisplayMiscellaneousOptions.None);

	private static string GetFormattedTypeName(INamedTypeSymbol type)
	{
		return type.ToDisplayString(BaseClassDisplayFormat);
	}

	private static ImmutableArray<Location> GetTypeParameterLocationsToReport(INamedTypeSymbol type)
	{
		var locations = type.DeclaringSyntaxReferences
			.Select(r => r.GetSyntax() as TypeDeclarationSyntax)
			.Select(s => s?.TypeParameterList?.GetLocation()!)
			.Where(l => l is not null && l.SourceSpan.Length > 0)
			.ToImmutableArray();

		if (locations.Length > 0)
		{
			return locations;
		}

		return type.Locations;
	}

	private static ImmutableArray<Location> GetPrimaryConstructorLocationsToReport(INamedTypeSymbol type)
	{
		var locations = type.DeclaringSyntaxReferences
			.Select(r => r.GetSyntax() as TypeDeclarationSyntax)
			.Select(s => s?.ParameterList?.GetLocation()!)
			.Where(l => l is not null && l.SourceSpan.Length > 0)
			.ToImmutableArray();

		if (locations.Length > 0)
		{
			return locations;
		}

		return type.Locations;
	}

	private static ImmutableArray<Location> GetBaseClassLocationsToReport(INamedTypeSymbol type)
	{
		var expectedBaseClassName = type.ContainingType?.Name;
		if (string.IsNullOrWhiteSpace(expectedBaseClassName))
		{
			return type.Locations;
		}

		var locations = type.DeclaringSyntaxReferences
			.Select(r => r.GetSyntax() as TypeDeclarationSyntax)
			.Select(s => s?.BaseList?.Types.FirstOrDefault())
			.Select(s => s switch
			{
				SimpleBaseTypeSyntax x => x.Type,
				PrimaryConstructorBaseTypeSyntax x => x.Type,
				_ => null,
			})
			.Where(t => t is not null && (t.ToString() == expectedBaseClassName || t.ToString().StartsWith(expectedBaseClassName + "<", StringComparison.Ordinal))) // Not foolproof, but good enough without having to summon the semantic model.
			.Select(t => t?.GetLocation()!)
			.Where(l => l is not null && l.SourceSpan.Length > 0)
			.ToImmutableArray();

		if (locations.Length > 0)
		{
			return locations;
		}

		return type.Locations;
	}

	private void ValidateNestedClass(SymbolAnalysisContext context, INamedTypeSymbol type)
	{
		if (type.TypeKind == TypeKind.Class && GetContainingTypeAsEnumClass(type) is not null)
		{
			ReportDiagnostic(context, type, Diagnostics.EC1030_UnrelatedNestedType);
		}
	}

	private static bool IsDeeplyNestedInside(INamedTypeSymbol parent, INamedTypeSymbol child)
	{
		var currentParent = child.ContainingType;
		while (currentParent is not null)
		{
			if (AreOfTheSameGenericType(currentParent, parent))
			{
				return true;
			}

			currentParent = currentParent.ContainingType;
		}

		return false;
	}

	/// <summary>
	/// Checks if the two symbols refer to the same type, ignoring any specific type arguments.
	/// </summary>
	private static bool AreOfTheSameGenericType(INamedTypeSymbol left, INamedTypeSymbol right)
	{
		return SymbolEqualityComparer.Default.Equals(left.OriginalDefinition, right.OriginalDefinition);
	}

	private static INamedTypeSymbol? GetBaseTypeAsEnumClass(INamedTypeSymbol type)
	{
		var baseType = type.BaseType;
		if (baseType is null)
		{
			return null;
		}

		if (!baseType.HasEnumClassAttribute())
		{
			return null;
		}

		return baseType;
	}

	private static INamedTypeSymbol? GetContainingTypeAsEnumClass(INamedTypeSymbol type)
	{
		var containingType = type.ContainingType;
		if (containingType is null)
		{
			return null;
		}

		if (!containingType.HasEnumClassAttribute())
		{
			return null;
		}

		return containingType;
	}

	private static void ReportDiagnostic(SymbolAnalysisContext context, ISymbol symbol, DiagnosticDescriptor diagnostic)
	{
		ReportDiagnostic(context, symbol.Locations, diagnostic);
	}

	private static void ReportDiagnostic(SymbolAnalysisContext context, ImmutableArray<Location> locations, DiagnosticDescriptor diagnostic, params string?[]? messageArgs)
	{
		foreach (var location in locations)
		{
			context.ReportDiagnostic(Diagnostic.Create(diagnostic, location, messageArgs));
		}
	}

	private static ConstructorKind GetConstructorKind(IMethodSymbol constructor)
	{
		var containingType = constructor.ContainingType;

		if (constructor.IsImplicitlyDeclared)
		{
			if (constructor.Parameters.Length == 0)
			{
				return ConstructorKind.ImplicitParameterless;
			}

			if (containingType.IsRecord && constructor.Parameters.Length == 1 && SymbolEqualityComparer.Default.Equals(constructor.Parameters[0].Type, containingType))
			{
				return ConstructorKind.ImplicitCopy;
			}

			return ConstructorKind.Primary;
		}
		else
		{
			// FIXME: find a more reliable way to check if `constructor` is the primary constructor besides just the number of parameters
			var primary = GetPrimaryConstructorParameterList(containingType);
			if (primary is not null && primary.Parameters.Count == constructor.Parameters.Length)
			{
				return ConstructorKind.Primary;
			}
			else
			{
				return ConstructorKind.Regular;
			}
		}
	}

	private static ParameterListSyntax? GetPrimaryConstructorParameterList(INamedTypeSymbol type)
	{
		foreach (var syntaxReference in type.DeclaringSyntaxReferences)
		{
			if (syntaxReference.GetSyntax() is TypeDeclarationSyntax t && t.ParameterList is var list and not null)
			{
				return list;
			}
		}

		return null;
	}

	private enum ConstructorKind
	{
		Regular,
		Primary,
		ImplicitParameterless,
		ImplicitCopy,
	}
}
