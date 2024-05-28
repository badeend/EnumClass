using Badeend.EnumClass.Internals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Badeend.EnumClass.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class SwitchExpressionAnalyzer : ExhaustivenessAnalyzer
{
	protected override DiagnosticDescriptor NotExhaustiveDiagnostic { get; } = Diagnostics.EC2001_NotExhaustive;

	public override sealed void Initialize(AnalysisContext context)
	{
		context.EnableConcurrentExecution(); // WARNING! Be careful not to store any state in the analyzer instances
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

		context.RegisterSyntaxNodeAction(this.AnalyzeSwitchExpression, SyntaxKind.SwitchExpression);
	}

	private void AnalyzeSwitchExpression(SyntaxNodeAnalysisContext context)
	{
		var switchExpression = (SwitchExpressionSyntax)context.Node;
		var patterns = new PatternParser(context.SemanticModel).Parse(switchExpression);

		this.AnalyzeSwitch(context, switchExpression.GoverningExpression, switchExpression.SwitchKeyword, patterns);
	}
}
