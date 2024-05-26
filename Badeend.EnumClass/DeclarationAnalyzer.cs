using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Badeend.EnumClass.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DeclarationAnalyzer : DiagnosticAnalyzer
{
	private static readonly DiagnosticDescriptor EnumClassMustBeAbstractDiagnostic = new(
		id: "EC1000",
		title: "Enum class must be abstract",
		messageFormat: "Enum class must be abstract",
		category: DiagnosticCategory.Declaration,
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true,
		helpLinkUri: "https://badeend.github.io/EnumClass/diagnostics/EC1000.html",
		customTags: [WellKnownDiagnosticTags.NotConfigurable]);

	private static readonly DiagnosticDescriptor EnumCaseOutsideEnumClassDiagnostic = new(
		id: "EC1001",
		title: "Cannot extend enum class outside of its definition",
		messageFormat: "Cannot extend enum class outside of its definition. Enum cases must be placed directly within their base class.",
		category: DiagnosticCategory.Declaration,
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true,
		helpLinkUri: "https://badeend.github.io/EnumClass/diagnostics/EC1001.html",
		customTags: [WellKnownDiagnosticTags.NotConfigurable]);

	private static readonly DiagnosticDescriptor EnumCaseVisibilityDiagnostic = new(
		id: "EC1002",
		title: "Enum case must be at least as visible as the containing enum class",
		messageFormat: "Enum case must be at least as visible as the containing enum class",
		category: DiagnosticCategory.Declaration,
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true,
		helpLinkUri: "https://badeend.github.io/EnumClass/diagnostics/EC1002.html",
		customTags: [WellKnownDiagnosticTags.NotConfigurable]);

	private static readonly DiagnosticDescriptor MissingParameterlessPrivateConstructorDiagnostic = new(
		id: "EC1003",
		title: "Enum class must declare a private constructor to prevent external extension",
		messageFormat: "Enum class must declare a private constructor to prevent external extension",
		category: DiagnosticCategory.Declaration,
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true,
		helpLinkUri: "https://badeend.github.io/EnumClass/diagnostics/EC1003.html",
		customTags: [WellKnownDiagnosticTags.NotConfigurable]);

	private static readonly DiagnosticDescriptor PrimaryConstructorsNotAllowedDiagnostic = new(
		id: "EC1004",
		title: "Primary constructor not allowed on enum class",
		messageFormat: "Primary constructor not allowed on enum class",
		category: DiagnosticCategory.Declaration,
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true,
		helpLinkUri: "https://badeend.github.io/EnumClass/diagnostics/EC1004.html",
		customTags: [WellKnownDiagnosticTags.NotConfigurable]);

	private static readonly DiagnosticDescriptor PublicConstructorsNotAllowedDiagnostic = new(
		id: "EC1005",
		title: "Externally accessible constructor not allowed on enum class",
		messageFormat: "Externally accessible constructor not allowed on enum class",
		category: DiagnosticCategory.Declaration,
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true,
		helpLinkUri: "https://badeend.github.io/EnumClass/diagnostics/EC1005.html",
		customTags: [WellKnownDiagnosticTags.NotConfigurable]);

	private static readonly DiagnosticDescriptor InvalidCasePlacementDiagnostic = new(
		id: "EC1006",
		title: "Incorrectly placed enum case",
		messageFormat: "Incorrectly placed enum case. Enum cases must be placed as a direct child of their base class.",
		category: DiagnosticCategory.Declaration,
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true,
		helpLinkUri: "https://badeend.github.io/EnumClass/diagnostics/EC1006.html",
		customTags: [WellKnownDiagnosticTags.NotConfigurable]);

	private static readonly DiagnosticDescriptor CaseTypeParametersDiagnostic = new(
		id: "EC1007",
		title: "Enum case may not declare type parameters",
		messageFormat: "Enum case may not declare type parameters. Any type parameter should be declared on the parent enum class.",
		category: DiagnosticCategory.Declaration,
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true,
		helpLinkUri: "https://badeend.github.io/EnumClass/diagnostics/EC1007.html",
		customTags: [WellKnownDiagnosticTags.NotConfigurable]);

	private static readonly DiagnosticDescriptor BaseTypeSpecializationDiagnostic = new(
		id: "EC1008",
		title: "Enum case must extend parent class verbatim",
		messageFormat: "Enum case must extend parent class verbatim. Expected base class to be `{0}`, found `{1}` instead.",
		category: DiagnosticCategory.Declaration,
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true,
		helpLinkUri: "https://badeend.github.io/EnumClass/diagnostics/EC1008.html",
		customTags: [WellKnownDiagnosticTags.NotConfigurable]);

	private static readonly DiagnosticDescriptor UnrelatedNestedTypeDiagnostic = new(
		id: "EC1030",
		title: "Nested type does not extend the enum class it is part of",
		messageFormat: "Nested type does not extend the enum class it is part of. Therefore, it will not be considered a \"case\" of the enum class. If this is intentional, you can safely suppress this warning.",
		category: DiagnosticCategory.Declaration,
		defaultSeverity: DiagnosticSeverity.Warning,
		isEnabledByDefault: true,
		helpLinkUri: "https://badeend.github.io/EnumClass/diagnostics/EC1030.html");

	private static readonly DiagnosticDescriptor NoCasesDiagnostic = new(
		id: "EC1031",
		title: "Enum class does not contain any cases",
		messageFormat: "Enum class does not contain any cases and can therefore not be instantiated",
		category: DiagnosticCategory.Declaration,
		defaultSeverity: DiagnosticSeverity.Warning,
		isEnabledByDefault: true,
		helpLinkUri: "https://badeend.github.io/EnumClass/diagnostics/EC1031.html");

	public override sealed ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [
		EnumClassMustBeAbstractDiagnostic,
		EnumCaseOutsideEnumClassDiagnostic,
		EnumCaseVisibilityDiagnostic,
		MissingParameterlessPrivateConstructorDiagnostic,
		PrimaryConstructorsNotAllowedDiagnostic,
		PublicConstructorsNotAllowedDiagnostic,
		InvalidCasePlacementDiagnostic,
		CaseTypeParametersDiagnostic,
		BaseTypeSpecializationDiagnostic,
		UnrelatedNestedTypeDiagnostic,
		NoCasesDiagnostic,
	];

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
			ReportDiagnostic(context, enumClass, EnumClassMustBeAbstractDiagnostic);
		}

		foreach (var constructor in enumClass.InstanceConstructors)
		{
			this.ValidatePublicConstructor(context, enumClass, constructor);
		}

		var hasCases = enumClass.GetTypeMembers().Any(c => c.BaseType is { } baseType && AreOfTheSameGenericType(enumClass, baseType));
		if (!hasCases)
		{
			ReportDiagnostic(context, enumClass, NoCasesDiagnostic);
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
					ReportDiagnostic(context, enumClass.Locations, MissingParameterlessPrivateConstructorDiagnostic);
					break;

				case ConstructorKind.Primary:
					ReportDiagnostic(context, GetPrimaryConstructorLocationsToReport(enumClass), PrimaryConstructorsNotAllowedDiagnostic);
					break;

				case ConstructorKind.Regular:
					ReportDiagnostic(context, constructor.Locations, PublicConstructorsNotAllowedDiagnostic);
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
				ReportDiagnostic(context, GetBaseClassLocationsToReport(enumCase), BaseTypeSpecializationDiagnostic, GetFormattedTypeName(enumCase.ContainingType.OriginalDefinition), GetFormattedTypeName(enumClass));
			}
			else if (IsDeeplyNestedInside(enumClass, enumCase))
			{
				ReportDiagnostic(context, enumCase, InvalidCasePlacementDiagnostic);
			}
			else
			{
				ReportDiagnostic(context, enumCase, EnumCaseOutsideEnumClassDiagnostic);
			}
		}

		if (enumCase.DeclaredAccessibility <= Accessibility.Private || enumCase.DeclaredAccessibility < enumClass.DeclaredAccessibility)
		{
			ReportDiagnostic(context, enumCase, EnumCaseVisibilityDiagnostic);
		}

		if (enumCase.TypeParameters.IsEmpty == false)
		{
			ReportDiagnostic(context, GetTypeParameterLocationsToReport(enumCase), CaseTypeParametersDiagnostic);
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
			ReportDiagnostic(context, type, UnrelatedNestedTypeDiagnostic);
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
