using System.Collections.Immutable;
using Badeend.EnumClass.Internals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Badeend.EnumClass.DiagnosticSuppressors;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class NonExhaustiveSwitchExpressionSuppressor : DiagnosticSuppressor
{
	/// <summary>
	/// CS8509: "The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern '...' is not covered".
	/// </summary>
	private static readonly SuppressionDescriptor CS8509Suppression = new(
		id: "CS8509_EnumClass",
		suppressedDiagnosticId: "CS8509",
		justification: "All enum cases were matched");

	/// <summary>
	/// IDE0072: "Add missing cases to switch expression".
	/// </summary>
	private static readonly SuppressionDescriptor IDE0072Suppression = new(
		id: "IDE0072_EnumClass",
		suppressedDiagnosticId: "IDE0072",
		justification: "All enum cases were matched");

	public override ImmutableArray<SuppressionDescriptor> SupportedSuppressions { get; } = ImmutableArray.Create([
		CS8509Suppression,
		IDE0072Suppression,
	]);

	public override void ReportSuppressions(SuppressionAnalysisContext context)
	{
		foreach (var diagnostic in context.ReportedDiagnostics)
		{
			var suppression = diagnostic.Id switch
			{
				"CS8509" => CS8509Suppression,
				"IDE0072" => IDE0072Suppression,
				_ => null,
			};

			if (suppression is not null)
			{
				TrySuppress(context, diagnostic, suppression);
			}
		}
	}

	private static void TrySuppress(SuppressionAnalysisContext context, Diagnostic diagnostic, SuppressionDescriptor suppression)
	{
		var node = diagnostic.Location.SourceTree?.GetRoot(context.CancellationToken).FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true);
		if (node is null)
		{
			return;
		}

		var switchExpression = node.AncestorsAndSelf().OfType<SwitchExpressionSyntax>().FirstOrDefault();
		if (switchExpression is null)
		{
			return;
		}

		var semanticModel = context.GetSemanticModel(node.SyntaxTree);

		var enumClassType = semanticModel.GetEnumClassType(switchExpression.GoverningExpression);
		if (enumClassType is null)
		{
			return;
		}

		context.ReportSuppression(Suppression.Create(suppression, diagnostic));
	}
}
