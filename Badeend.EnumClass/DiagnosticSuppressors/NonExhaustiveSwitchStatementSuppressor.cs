using System.Collections.Immutable;
using Badeend.EnumClass.Internals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Badeend.EnumClass.DiagnosticSuppressors;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class NonExhaustiveSwitchStatementSuppressor : DiagnosticSuppressor
{
	/// <summary>
	/// IDE0010: "Add missing cases to switch statement".
	/// </summary>
	private static readonly SuppressionDescriptor IDE0010Suppression = new(
		id: "IDE0010_EnumClass",
		suppressedDiagnosticId: "IDE0010",
		justification: "All enum cases were matched");

	public override ImmutableArray<SuppressionDescriptor> SupportedSuppressions { get; } = ImmutableArray.Create([
		IDE0010Suppression,
	]);

	public override void ReportSuppressions(SuppressionAnalysisContext context)
	{
		foreach (var diagnostic in context.ReportedDiagnostics)
		{
			var suppression = diagnostic.Id switch
			{
				"IDE0010" => IDE0010Suppression,
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

		var switchStatement = node.AncestorsAndSelf().OfType<SwitchStatementSyntax>().FirstOrDefault();
		if (switchStatement is null)
		{
			return;
		}

		var semanticModel = context.GetSemanticModel(node.SyntaxTree);

		var enumClassType = semanticModel.GetEnumClassType(switchStatement.Expression);
		if (enumClassType is null)
		{
			return;
		}

		context.ReportSuppression(Suppression.Create(suppression, diagnostic));
	}
}
