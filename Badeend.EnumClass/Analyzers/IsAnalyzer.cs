using System.Collections.Immutable;
using Badeend.EnumClass.Internals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Badeend.EnumClass.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class IsAnalyzer : DiagnosticAnalyzer
{
	public override sealed ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create([
		Diagnostics.EC2003_UnreachablePattern,
		Diagnostics.EC2004_NoCaseImplementsInterface,
	]);

	public override sealed void Initialize(AnalysisContext context)
	{
		context.EnableConcurrentExecution(); // WARNING! Be careful not to store any state in the analyzer instances
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

		context.RegisterSyntaxNodeAction(this.AnalyzeIsExpression, SyntaxKind.IsExpression);
		context.RegisterSyntaxNodeAction(this.AnalyzeIsPatternExpression, SyntaxKind.IsPatternExpression);
	}

	private void AnalyzeIsExpression(SyntaxNodeAnalysisContext context)
	{
		var expression = (BinaryExpressionSyntax)context.Node;

		// Only run analysis for the side effects. We don't care about exhaustiveness in `is` expressions.
		_ = CreateCaseAnalyzer(context).Analyze(expression);
	}

	private void AnalyzeIsPatternExpression(SyntaxNodeAnalysisContext context)
	{
		var expression = (IsPatternExpressionSyntax)context.Node;

		// Only run analysis for the side effects. We don't care about exhaustiveness in `is` expressions.
		_ = CreateCaseAnalyzer(context).Analyze(expression);
	}

	private static CaseAnalyzer CreateCaseAnalyzer(SyntaxNodeAnalysisContext context)
	{
		return new CaseAnalyzer(
			semanticModel: context.SemanticModel,
			reportUnreachablePattern: (location, message) => ReportDiagnostic(context, location, Diagnostics.EC2003_UnreachablePattern, message),
			reportNoCaseImplementsInterface: (location) => ReportDiagnostic(context, location, Diagnostics.EC2004_NoCaseImplementsInterface));
	}

	private static void ReportDiagnostic(SyntaxNodeAnalysisContext context, Location location, DiagnosticDescriptor diagnostic, params string?[]? messageArgs)
	{
		context.ReportDiagnostic(Diagnostic.Create(diagnostic, location, messageArgs));
	}
}
