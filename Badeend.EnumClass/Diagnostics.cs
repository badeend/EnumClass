using Microsoft.CodeAnalysis;

namespace Badeend.EnumClass;

#pragma warning disable SA1310 // Field names should not contain underscore

internal static class Diagnostics
{
	private static class Category
	{
		public const string Declaration = nameof(Declaration);
		public const string Usage = nameof(Usage);
		public const string Miscellaneous = nameof(Miscellaneous);
	}

	internal const string EC1000 = nameof(EC1000);
	internal const string EC1001 = nameof(EC1001);
	internal const string EC1002 = nameof(EC1002);
	internal const string EC1003 = nameof(EC1003);
	internal const string EC1004 = nameof(EC1004);
	internal const string EC1005 = nameof(EC1005);
	internal const string EC1006 = nameof(EC1006);
	internal const string EC1007 = nameof(EC1007);
	internal const string EC1008 = nameof(EC1008);
	internal const string EC1030 = nameof(EC1030);
	internal const string EC1031 = nameof(EC1031);
	internal const string EC2001 = nameof(EC2001);
	internal const string EC2002 = nameof(EC2002);
	internal const string EC2003 = nameof(EC2003);
	internal const string EC2004 = nameof(EC2004);
	internal const string EC3001 = nameof(EC3001);
	internal const string EC3002 = nameof(EC3002);

	internal static readonly DiagnosticDescriptor EC1000_EnumClassMustBeAbstract = new(
		id: EC1000,
		title: "Enum class must be abstract",
		messageFormat: "Enum class must be abstract",
		category: Category.Declaration,
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true,
		helpLinkUri: $"https://badeend.github.io/EnumClass/diagnostics/{EC1000}.html",
		customTags: [WellKnownDiagnosticTags.NotConfigurable]);

	internal static readonly DiagnosticDescriptor EC1001_EnumCaseOutsideEnumClass = new(
		id: EC1001,
		title: "Cannot extend enum class outside of its definition",
		messageFormat: "Cannot extend enum class outside of its definition. Enum cases must be placed directly within their base class.",
		category: Category.Declaration,
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true,
		helpLinkUri: $"https://badeend.github.io/EnumClass/diagnostics/{EC1001}.html",
		customTags: [WellKnownDiagnosticTags.NotConfigurable]);

	internal static readonly DiagnosticDescriptor EC1002_EnumCaseVisibility = new(
		id: EC1002,
		title: "Enum case must be at least as visible as the containing enum class",
		messageFormat: "Enum case must be at least as visible as the containing enum class",
		category: Category.Declaration,
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true,
		helpLinkUri: $"https://badeend.github.io/EnumClass/diagnostics/{EC1002}.html",
		customTags: [WellKnownDiagnosticTags.NotConfigurable]);

	internal static readonly DiagnosticDescriptor EC1003_MissingParameterlessPrivateConstructor = new(
		id: EC1003,
		title: "Enum class must declare a private constructor to prevent external extension",
		messageFormat: "Enum class must declare a private constructor to prevent external extension",
		category: Category.Declaration,
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true,
		helpLinkUri: $"https://badeend.github.io/EnumClass/diagnostics/{EC1003}.html",
		customTags: [WellKnownDiagnosticTags.NotConfigurable]);

	internal static readonly DiagnosticDescriptor EC1004_PrimaryConstructorsNotAllowed = new(
		id: EC1004,
		title: "Primary constructor not allowed on enum class",
		messageFormat: "Primary constructor not allowed on enum class",
		category: Category.Declaration,
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true,
		helpLinkUri: $"https://badeend.github.io/EnumClass/diagnostics/{EC1004}.html",
		customTags: [WellKnownDiagnosticTags.NotConfigurable]);

	internal static readonly DiagnosticDescriptor EC1005_PublicConstructorsNotAllowed = new(
		id: EC1005,
		title: "Externally accessible constructor not allowed on enum class",
		messageFormat: "Externally accessible constructor not allowed on enum class",
		category: Category.Declaration,
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true,
		helpLinkUri: $"https://badeend.github.io/EnumClass/diagnostics/{EC1005}.html",
		customTags: [WellKnownDiagnosticTags.NotConfigurable]);

	internal static readonly DiagnosticDescriptor EC1006_InvalidCasePlacement = new(
		id: EC1006,
		title: "Incorrectly placed enum case",
		messageFormat: "Incorrectly placed enum case. Enum cases must be placed as a direct child of their base class.",
		category: Category.Declaration,
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true,
		helpLinkUri: $"https://badeend.github.io/EnumClass/diagnostics/{EC1006}.html",
		customTags: [WellKnownDiagnosticTags.NotConfigurable]);

	internal static readonly DiagnosticDescriptor EC1007_CaseTypeParameters = new(
		id: EC1007,
		title: "Enum case may not declare type parameters",
		messageFormat: "Enum case may not declare type parameters. Any type parameter should be declared on the parent enum class.",
		category: Category.Declaration,
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true,
		helpLinkUri: $"https://badeend.github.io/EnumClass/diagnostics/{EC1007}.html",
		customTags: [WellKnownDiagnosticTags.NotConfigurable]);

	internal static readonly DiagnosticDescriptor EC1008_BaseTypeSpecialization = new(
		id: EC1008,
		title: "Enum case must extend parent class verbatim",
		messageFormat: "Enum case must extend parent class verbatim. Expected base class to be `{0}`, found `{1}` instead.",
		category: Category.Declaration,
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true,
		helpLinkUri: $"https://badeend.github.io/EnumClass/diagnostics/{EC1008}.html",
		customTags: [WellKnownDiagnosticTags.NotConfigurable]);

	internal static readonly DiagnosticDescriptor EC1030_UnrelatedNestedType = new(
		id: EC1030,
		title: "Nested type does not extend the enum class it is part of",
		messageFormat: "Nested type does not extend the enum class it is part of. Therefore, it will not be considered a \"case\" of the enum class. If this is intentional, you can safely suppress this warning.",
		category: Category.Declaration,
		defaultSeverity: DiagnosticSeverity.Warning,
		isEnabledByDefault: true,
		helpLinkUri: $"https://badeend.github.io/EnumClass/diagnostics/{EC1030}.html");

	internal static readonly DiagnosticDescriptor EC1031_NoCases = new(
		id: EC1031,
		title: "Enum class does not contain any cases",
		messageFormat: "Enum class does not contain any cases and can therefore not be instantiated",
		category: Category.Declaration,
		defaultSeverity: DiagnosticSeverity.Warning,
		isEnabledByDefault: true,
		helpLinkUri: $"https://badeend.github.io/EnumClass/diagnostics/{EC1031}.html");

	internal static readonly DiagnosticDescriptor EC2001_NotExhaustive = new(
		id: EC2001,
		title: "Switch expression on enum class is not exhaustive",
		messageFormat: "Switch is not exhaustive. {0}",
		category: Category.Usage,
		defaultSeverity: DiagnosticSeverity.Warning,
		isEnabledByDefault: true,
		helpLinkUri: $"https://badeend.github.io/EnumClass/diagnostics/{EC2001}.html");

	internal static readonly DiagnosticDescriptor EC2002_NotExhaustive = new(
		id: EC2002,
		title: "Switch statement on enum class is not exhaustive",
		messageFormat: "Switch is not exhaustive. {0}",
		category: Category.Usage,
		defaultSeverity: DiagnosticSeverity.Warning,
		isEnabledByDefault: true,
		helpLinkUri: $"https://badeend.github.io/EnumClass/diagnostics/{EC2002}.html");

	internal static readonly DiagnosticDescriptor EC2003_UnreachablePattern = new(
		id: EC2003,
		title: "Unreachable pattern",
		messageFormat: "Unreachable pattern. {0}",
		category: Category.Usage,
		defaultSeverity: DiagnosticSeverity.Warning,
		isEnabledByDefault: true,
		helpLinkUri: $"https://badeend.github.io/EnumClass/diagnostics/{EC2003}.html");

	internal static readonly DiagnosticDescriptor EC2004_NoCaseImplementsInterface = new(
		id: EC2004,
		title: "None of the enum cases implement this interface",
		messageFormat: "None of the enum cases implement this interface",
		category: Category.Usage,
		defaultSeverity: DiagnosticSeverity.Warning,
		isEnabledByDefault: true,
		helpLinkUri: $"https://badeend.github.io/EnumClass/diagnostics/{EC2004}.html");

	internal static readonly DiagnosticDescriptor EC3001_JsonDiscriminatorOnNonEnumCase = new(
		id: EC3001,
		title: "Useless [JsonDiscriminator] attribute",
		messageFormat: "The [JsonDiscriminator] attribute only applies to enum class cases and won't have any effect here",
		category: Category.Miscellaneous,
		defaultSeverity: DiagnosticSeverity.Warning,
		isEnabledByDefault: true,
		helpLinkUri: $"https://badeend.github.io/EnumClass/diagnostics/{EC3001}.html");

	internal static readonly DiagnosticDescriptor EC3002_JsonDiscriminatorOnNestedEnumClass = new(
		id: EC3002,
		title: "Useless [JsonDiscriminator] attribute",
		messageFormat: "The [JsonDiscriminator] attribute does not have any effect when placed on the base type of a nested enum class. If you want to customize the discriminator, annotate the individual sub-cases instead.",
		category: Category.Miscellaneous,
		defaultSeverity: DiagnosticSeverity.Warning,
		isEnabledByDefault: true,
		helpLinkUri: $"https://badeend.github.io/EnumClass/diagnostics/{EC3002}.html");
}
