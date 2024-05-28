using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Badeend.EnumClass.Internals;

internal sealed class PatternParser(SemanticModel semanticModel)
{
	internal IEnumerable<Pattern> Parse(IsPatternExpressionSyntax isExpression)
	{
		return this.Parse(isExpression.Pattern);
	}

	internal IEnumerable<Pattern> Parse(SwitchExpressionSyntax switchExpression)
	{
		return switchExpression.Arms.SelectMany(arm => ApplyWhenClause(this.Parse(arm.Pattern), arm.WhenClause));
	}

	internal IEnumerable<Pattern> Parse(SwitchStatementSyntax switchStatement)
	{
		return switchStatement.Sections.SelectMany(section => section.Labels).SelectMany(label => label switch
		{
			DefaultSwitchLabelSyntax defaultSwitchLabel => [new Pattern.Wildcard(defaultSwitchLabel)],
			CaseSwitchLabelSyntax caseSwitchLabel => this.ParseExpression(caseSwitchLabel.Value, partial: false),
			CasePatternSwitchLabelSyntax casePatternSwitchLabel => ApplyWhenClause(this.Parse(casePatternSwitchLabel.Pattern), casePatternSwitchLabel.WhenClause),

			_ => [new Pattern.Other(label)],
		});
	}

	private static IEnumerable<Pattern> ApplyWhenClause(IEnumerable<Pattern> patterns, WhenClauseSyntax? whenClause)
	{
		if (whenClause is null)
		{
			return patterns;
		}

		return patterns.Select(pattern => pattern switch
		{
			Pattern.TypeCheck typeCheck => typeCheck with { IsPartial = true },
			_ => (Pattern)new Pattern.Other(pattern.Node),
		});
	}

	internal IEnumerable<Pattern> Parse(PatternSyntax pattern) => pattern switch
	{
		DiscardPatternSyntax => [new Pattern.Wildcard(pattern)],
		ParenthesizedPatternSyntax parenthesizedPattern => this.Parse(parenthesizedPattern.Pattern),
		TypePatternSyntax typePattern => this.ParseExpression(typePattern.Type, partial: false),
		VarPatternSyntax => [new Pattern.Wildcard(pattern)],
		DeclarationPatternSyntax declarationPattern => this.ParseExpression(declarationPattern.Type, partial: false),
		BinaryPatternSyntax binaryPattern => this.ParseBinaryPattern(binaryPattern),
		ConstantPatternSyntax constantPattern => this.ParseExpression(constantPattern.Expression, partial: false),
		RecursivePatternSyntax recursivePattern => this.ParseRecursivePattern(recursivePattern),

		ListPatternSyntax or
		RelationalPatternSyntax or
		SlicePatternSyntax or
		UnaryPatternSyntax or
		_ => [new Pattern.Other(pattern)],
	};

	internal IEnumerable<Pattern> ParseExpression(ExpressionSyntax expression, bool partial)
	{
		if (IsNullLiteral(expression))
		{
			return [new Pattern.Null(expression)];
		}

		if (semanticModel.GetSymbolInfo(expression).Symbol is INamedTypeSymbol type)
		{
			return [new Pattern.TypeCheck(expression, type, partial)];
		}

		return [new Pattern.Other(expression)];
	}

	private IEnumerable<Pattern> ParseBinaryPattern(BinaryPatternSyntax binaryPattern)
	{
		if (binaryPattern.OperatorToken.IsKind(SyntaxKind.OrKeyword))
		{
			return this.ParseOrPattern(binaryPattern);
		}
		else if (binaryPattern.OperatorToken.IsKind(SyntaxKind.AndKeyword))
		{
			return this.ParseAndPattern(binaryPattern);
		}
		else
		{
			// Unknown keyword. Assume the worst case:
			return [new Pattern.Other(binaryPattern)];
		}
	}

	private IEnumerable<Pattern> ParseOrPattern(BinaryPatternSyntax orPattern)
	{
		return this.Parse(orPattern.Left).Concat(this.Parse(orPattern.Right));
	}

	private IEnumerable<Pattern> ParseAndPattern(BinaryPatternSyntax andPattern)
	{
		if (this.IsWildcard(andPattern.Left) && this.IsWildcard(andPattern.Right))
		{
			return [new Pattern.Wildcard(andPattern)];
		}

		return [new Pattern.Other(andPattern)];
	}

	private IEnumerable<Pattern> ParseRecursivePattern(RecursivePatternSyntax recursivePattern)
	{
		if (recursivePattern.Type is not null)
		{
			var positionalPatternsAreWildcard = recursivePattern.PositionalPatternClause?.Subpatterns.All(p => this.IsWildcard(p.Pattern)) ?? true;
			var propertyPatternsAreWildcard = recursivePattern.PropertyPatternClause?.Subpatterns.All(p => this.IsWildcard(p.Pattern)) ?? true;

			if (positionalPatternsAreWildcard && propertyPatternsAreWildcard)
			{
				return this.ParseExpression(recursivePattern.Type, partial: false);
			}
			else
			{
				return this.ParseExpression(recursivePattern.Type, partial: true);
			}
		}
		else
		{
			if (recursivePattern.PositionalPatternClause is null && recursivePattern.PropertyPatternClause is null)
			{
				return [new Pattern.Wildcard(recursivePattern)];
			}
			else
			{
				return [new Pattern.Other(recursivePattern)];
			}
		}
	}

	private bool IsWildcard(PatternSyntax pattern)
	{
		return this.Parse(pattern).All(p => p is Pattern.Wildcard);
	}

	private static bool IsNullLiteral(ExpressionSyntax expression)
	{
		return expression is LiteralExpressionSyntax literalExpression && literalExpression.Token.IsKind(SyntaxKind.NullKeyword);
	}
}
