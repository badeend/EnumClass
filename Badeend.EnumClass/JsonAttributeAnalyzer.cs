using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Badeend.EnumClass.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class JsonAttributeAnalyzer : DiagnosticAnalyzer
{
	private static readonly DiagnosticDescriptor JsonDiscriminatorOnNonEnumCaseDiagnostic = new(
		id: "EC3001",
		title: "Useless [JsonDiscriminator] attribute",
		messageFormat: "The [JsonDiscriminator] attribute only applies to enum class cases and won't have any effect here",
		category: DiagnosticCategory.Miscellaneous,
		defaultSeverity: DiagnosticSeverity.Warning,
		isEnabledByDefault: true,
		helpLinkUri: "https://badeend.github.io/EnumClass/diagnostics/EC3001.html");

	private static readonly DiagnosticDescriptor JsonDiscriminatorOnNestedEnumClassDiagnostic = new(
		id: "EC3002",
		title: "Useless [JsonDiscriminator] attribute",
		messageFormat: "The [JsonDiscriminator] attribute does not have any effect when placed on the base type of a nested enum class. If you want to customize the discriminator, annotate the individual sub-cases instead.",
		category: DiagnosticCategory.Miscellaneous,
		defaultSeverity: DiagnosticSeverity.Warning,
		isEnabledByDefault: true,
		helpLinkUri: "https://badeend.github.io/EnumClass/diagnostics/EC3002.html");

	public override sealed ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create([
		JsonDiscriminatorOnNonEnumCaseDiagnostic,
		JsonDiscriminatorOnNestedEnumClassDiagnostic,
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

		var attribute = GetJsonDiscriminatorAttribute(type);
		if (attribute is null)
		{
			return;
		}

		var location = (attribute.ApplicationSyntaxReference?.GetSyntax() as AttributeSyntax)?.GetLocation();
		if (location is null)
		{
			return;
		}

		var baseType = type.BaseType;
		if (baseType is null || !baseType.HasEnumClassAttribute())
		{
			ReportDiagnostic(context, location, JsonDiscriminatorOnNonEnumCaseDiagnostic);
		}
		else if (type.HasEnumClassAttribute())
		{
			ReportDiagnostic(context, location, JsonDiscriminatorOnNestedEnumClassDiagnostic);
		}
	}

	private static AttributeData? GetJsonDiscriminatorAttribute(INamedTypeSymbol typeSymbol)
	{
		if (typeSymbol.TypeKind != TypeKind.Class)
		{
			// `Badeend.JsonDiscriminatorAttribute` has `AttributeTargets.Class`, so we
			// can bail fast if the symbol is not a class. If the attribute is
			// applied to a non-class, the C# compiler will handle that for us.
			return null;
		}

		return typeSymbol.GetAttributes().FirstOrDefault(IsJsonDiscriminatorAttribute);
	}

	private static bool IsJsonDiscriminatorAttribute(AttributeData attribute)
	{
		var attributeClass = attribute.AttributeClass;
		if (attributeClass is null)
		{
			return false;
		}

		return attributeClass.Name == "JsonDiscriminatorAttribute" && attributeClass.ContainingNamespace.ToString() == "Badeend.EnumClass.SystemTextJson";
	}

	private static void ReportDiagnostic(SymbolAnalysisContext context, Location location, DiagnosticDescriptor diagnostic, params string?[]? messageArgs)
	{
		context.ReportDiagnostic(Diagnostic.Create(diagnostic, location, messageArgs));
	}
}
