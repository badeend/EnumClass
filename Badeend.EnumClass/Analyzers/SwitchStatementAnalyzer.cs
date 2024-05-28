using Badeend.EnumClass.Internals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Badeend.EnumClass.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class SwitchStatementAnalyzer : ExhaustivenessAnalyzer
{
	protected override DiagnosticDescriptor NotExhaustiveDiagnostic { get; } = Diagnostics.EC2002_NotExhaustive;

	public override sealed void Initialize(AnalysisContext context)
	{
		context.EnableConcurrentExecution(); // WARNING! Be careful not to store any state in the analyzer instances
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

		context.RegisterSyntaxNodeAction(this.AnalyzeSwitchStatement, SyntaxKind.SwitchStatement);
	}

	private void AnalyzeSwitchStatement(SyntaxNodeAnalysisContext context)
	{
		var switchStatement = (SwitchStatementSyntax)context.Node;
		var patterns = new PatternParser(context.SemanticModel).Parse(switchStatement);

		this.AnalyzeSwitch(context, switchStatement.Expression, switchStatement.SwitchKeyword, patterns);
	}
}
