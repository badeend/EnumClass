using System.Collections.Immutable;
using System.Composition;
using Badeend.EnumClass.Internals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;

namespace Badeend.EnumClass.CodeFixes;

[ExportCodeFixProvider(LanguageNames.CSharp)]
[Shared]
public sealed class EC2001_EC2002CodeFix : BaseCodeFix
{
	public override sealed ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create([
		Diagnostics.EC2001,
		Diagnostics.EC2002,
	]);

	protected override void SetUpCodeFixes(CodeFixContext context, SyntaxNode node)
	{
		switch (context.Diagnostics[0].Id)
		{
			case Diagnostics.EC2001:
			{
				var switchExpression = node.FirstAncestorOrSelf<SwitchExpressionSyntax>();
				if (switchExpression is null)
				{
					return;
				}

				this.RegisterCodeFix(context, "Add remaining cases", editor => AddRemainingCases(editor, switchExpression));
				break;
			}

			case Diagnostics.EC2002:
			{
				var switchStatement = node.FirstAncestorOrSelf<SwitchStatementSyntax>();
				if (switchStatement is null)
				{
					return;
				}

				this.RegisterCodeFix(context, "Add remaining cases", editor => AddRemainingCases(editor, switchStatement));
				break;
			}
		}
	}

	private static CaseAnalyzer CreateCaseAnalyzer(DocumentEditor editor)
	{
		return new CaseAnalyzer(
			semanticModel: editor.SemanticModel,
			reportUnreachablePattern: (location, message) => { /* Do nothing */ },
			reportNoCaseImplementsInterface: (location) => { /* Do nothing */ });
	}

	private static IEnumerable<PatternSyntax> GetPatternsToAdd(DocumentEditor editor, CaseAnalysis analysis, SyntaxNode referenceNode)
	{
		var patterns = analysis.Cases
			.Where(c => c.Match != CaseAnalysis.CaseMatchState.Full)
			.Select(c => DeclarationPattern(
				type: EnumCaseTypeSyntax(editor.SemanticModel, referenceNode, c.Type),
				identifier: c.Type.Name.ToCamelCase()) as PatternSyntax);

		if (analysis.IsMissingNullCheck)
		{
			patterns = new[] { NullPattern() }.Concat(patterns);
		}

		return patterns;
	}

	private static void AddRemainingCases(DocumentEditor editor, SwitchExpressionSyntax switchExpression)
	{
		var analysis = CreateCaseAnalyzer(editor).Analyze(switchExpression);
		if (analysis is null)
		{
			return;
		}

		var newArms = GetPatternsToAdd(editor, analysis, switchExpression).Select(DummySwitchExpressionArm).ToArray();
		if (newArms.Length == 0)
		{
			return;
		}

		var syntaxList = switchExpression.Arms
			.AddRange(newArms.ToArray())
			.GetWithSeparators()
			.Add(SyntaxFactory.Token(SyntaxKind.CommaToken));

		editor.ReplaceNode(switchExpression, switchExpression.WithArms(SyntaxFactory.SeparatedList<SwitchExpressionArmSyntax>(syntaxList)));
	}

	private static void AddRemainingCases(DocumentEditor editor, SwitchStatementSyntax switchStatement)
	{
		var analysis = CreateCaseAnalyzer(editor).Analyze(switchStatement);
		if (analysis is null)
		{
			return;
		}

		var newSections = GetPatternsToAdd(editor, analysis, switchStatement).Select(DummySwitchSection).ToArray();
		if (newSections.Length == 0)
		{
			return;
		}

		editor.ReplaceNode(switchStatement, switchStatement.AddSections(newSections.ToArray()));
	}

	private static SwitchExpressionArmSyntax DummySwitchExpressionArm(PatternSyntax pattern)
	{
		return SyntaxFactory.SwitchExpressionArm(
			pattern: pattern,
			expression: SyntaxFactory.ParseExpression(string.Empty).WithLeadingTrivia(SyntaxFactory.Whitespace(" ")))
			.WithAdditionalAnnotations(Formatter.Annotation);
	}

	private static SwitchSectionSyntax DummySwitchSection(PatternSyntax pattern)
	{
		return SyntaxFactory.SwitchSection(
			labels: new([
				SyntaxFactory.CasePatternSwitchLabel(pattern, SyntaxFactory.Token(SyntaxKind.ColonToken)),
			]),
			statements: new([
				SyntaxFactory.ParseStatement("TODO;"),
				SyntaxFactory.BreakStatement(),
			]))
			.WithAdditionalAnnotations(Formatter.Annotation);
	}

	private static ConstantPatternSyntax NullPattern()
	{
		return SyntaxFactory.ConstantPattern(SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression));
	}

	private static DeclarationPatternSyntax DeclarationPattern(TypeSyntax type, string identifier)
	{
		return SyntaxFactory.DeclarationPattern(type, SyntaxFactory.SingleVariableDesignation(SyntaxFactory.Identifier(identifier)));
	}

	private static TypeSyntax EnumCaseTypeSyntax(SemanticModel semanticModel, SyntaxNode referenceNode, INamedTypeSymbol enumCase)
	{
		return SyntaxFactory.ParseTypeName(enumCase.ToMinimalDisplayString(semanticModel, NullableFlowState.NotNull, referenceNode.GetLocation().SourceSpan.Start));
	}
}
