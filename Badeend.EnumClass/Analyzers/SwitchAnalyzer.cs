using System.Collections.Immutable;
using Badeend.EnumClass.Internals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Badeend.EnumClass.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class SwitchAnalyzer : DiagnosticAnalyzer
{
	public override sealed ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create([
		Diagnostics.EC2001_NotExhaustive,
		Diagnostics.EC2002_NotExhaustive,
		Diagnostics.EC2003_UnreachablePattern,
		Diagnostics.EC2004_NoCaseImplementsInterface,
	]);

	public override sealed void Initialize(AnalysisContext context)
	{
		context.EnableConcurrentExecution(); // WARNING! Be careful not to store any state in the analyzer instances
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

		context.RegisterSyntaxNodeAction(this.AnalyzeSwitchExpression, SyntaxKind.SwitchExpression);
		context.RegisterSyntaxNodeAction(this.AnalyzeSwitchStatement, SyntaxKind.SwitchStatement);
	}

	private void AnalyzeSwitchExpression(SyntaxNodeAnalysisContext context)
	{
		var switchExpression = (SwitchExpressionSyntax)context.Node;

		var analysis = CreateCaseAnalyzer(context).Analyze(switchExpression);

		if (analysis is not null)
		{
			ReportAnalysis(context, analysis, switchExpression.SwitchKeyword, Diagnostics.EC2001_NotExhaustive);
		}
	}

	private void AnalyzeSwitchStatement(SyntaxNodeAnalysisContext context)
	{
		var switchStatement = (SwitchStatementSyntax)context.Node;

		var analysis = CreateCaseAnalyzer(context).Analyze(switchStatement);

		if (analysis is not null)
		{
			ReportAnalysis(context, analysis, switchStatement.SwitchKeyword, Diagnostics.EC2002_NotExhaustive);
		}
	}

	private static CaseAnalyzer CreateCaseAnalyzer(SyntaxNodeAnalysisContext context)
	{
		return new CaseAnalyzer(
			compilation: context.Compilation,
			semanticModel: context.SemanticModel,
			reportUnreachablePattern: (location, message) => ReportDiagnostic(context, location, Diagnostics.EC2003_UnreachablePattern, message),
			reportNoCaseImplementsInterface: (location) => ReportDiagnostic(context, location, Diagnostics.EC2004_NoCaseImplementsInterface));
	}

	private static void ReportAnalysis(SyntaxNodeAnalysisContext context, CaseAnalysis analysis, SyntaxToken switchKeyword, DiagnosticDescriptor notExhaustiveDiagnostic)
	{
		if (analysis.IsMissingNullCheck)
		{
			ReportDiagnostic(context, switchKeyword, notExhaustiveDiagnostic, "The value being switched on can be null, but none of the arms check for it");
		}

		var unmatchedCases = analysis.Cases.Where(c => c.Match != CaseAnalysis.CaseMatchState.Full).Select(c => c.Type).ToArray();
		if (unmatchedCases.Length > 0)
		{
			var partiallyMatchedCases = analysis.Cases.Where(c => c.Match == CaseAnalysis.CaseMatchState.Partial).Select(c => c.Type).ToArray();
			if (partiallyMatchedCases.Length == unmatchedCases.Length)
			{
				ReportDiagnostic(context, switchKeyword, notExhaustiveDiagnostic, $"The following cases are being matched on, but only partially: {FormatCaseListForHumans(analysis.EnumClassType, partiallyMatchedCases)}");
			}
			else if (partiallyMatchedCases.Length > 0)
			{
				ReportDiagnostic(context, switchKeyword, notExhaustiveDiagnostic, $"Unhandled cases: {FormatCaseListForHumans(analysis.EnumClassType, unmatchedCases)}. Some of these are already being matched on, but only partially: {FormatCaseListForHumans(analysis.EnumClassType, partiallyMatchedCases)}");
			}
			else
			{
				ReportDiagnostic(context, switchKeyword, notExhaustiveDiagnostic, $"Unhandled cases: {FormatCaseListForHumans(analysis.EnumClassType, unmatchedCases)}");
			}
		}
	}

	private static readonly SymbolDisplayFormat CaseDisplayFormat = new(
		globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
		typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypes,
		genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
		memberOptions: SymbolDisplayMemberOptions.None,
		delegateStyle: SymbolDisplayDelegateStyle.NameOnly,
		extensionMethodStyle: SymbolDisplayExtensionMethodStyle.Default,
		parameterOptions: SymbolDisplayParameterOptions.None,
		propertyStyle: SymbolDisplayPropertyStyle.NameOnly,
		localOptions: SymbolDisplayLocalOptions.None,
		kindOptions: SymbolDisplayKindOptions.None,
		miscellaneousOptions: SymbolDisplayMiscellaneousOptions.None);

	private static string FormatCaseListForHumans(INamedTypeSymbol enumClassType, IReadOnlyCollection<INamedTypeSymbol> cases)
	{
		var commonPrefix = enumClassType.ToDisplayString(CaseDisplayFormat) + ".";

		var caseNames = cases.Select(c =>
		{
			var caseName = c.ToDisplayString(CaseDisplayFormat);
			if (caseName.StartsWith(commonPrefix))
			{
				return caseName.Substring(commonPrefix.Length);
			}
			else
			{
				return caseName;
			}
		});

		const int AbbreviateThreshold = 3;

		if (cases.Count <= AbbreviateThreshold)
		{
			return string.Join(", ", caseNames);
		}
		else
		{
			return string.Join(", ", caseNames.Take(AbbreviateThreshold)) + $", and {cases.Count - AbbreviateThreshold} more";
		}
	}

	private static void ReportDiagnostic(SyntaxNodeAnalysisContext context, SyntaxToken token, DiagnosticDescriptor diagnostic, params string?[]? messageArgs)
	{
		ReportDiagnostic(context, token.GetLocation(), diagnostic, messageArgs);
	}

	private static void ReportDiagnostic(SyntaxNodeAnalysisContext context, Location location, DiagnosticDescriptor diagnostic, params string?[]? messageArgs)
	{
		context.ReportDiagnostic(Diagnostic.Create(diagnostic, location, messageArgs));
	}
}
