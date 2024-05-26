using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Badeend.EnumClass.Analyzers;

public abstract class ExhaustivenessAnalyzer : DiagnosticAnalyzer
{
	protected abstract DiagnosticDescriptor NotExhaustiveDiagnostic { get; }

	[System.Diagnostics.CodeAnalysis.SuppressMessage("MicrosoftCodeAnalysisDesign", "RS1032:Define diagnostic message correctly", Justification = "The individual reports end with a period in their message")]
	private static readonly DiagnosticDescriptor UnreachablePatternDiagnostic = new(
		id: "EC2003",
		title: "Unreachable pattern",
		messageFormat: "Unreachable pattern. {0}",
		category: DiagnosticCategory.Usage,
		defaultSeverity: DiagnosticSeverity.Warning,
		isEnabledByDefault: true,
		helpLinkUri: "https://badeend.github.io/EnumClass/diagnostics/EC2003.html");

	private static readonly DiagnosticDescriptor NoCaseImplementsInterfaceDiagnostic = new(
		id: "EC2004",
		title: "None of the enum cases implement this interface",
		messageFormat: "None of the enum cases implement this interface",
		category: DiagnosticCategory.Usage,
		defaultSeverity: DiagnosticSeverity.Warning,
		isEnabledByDefault: true,
		helpLinkUri: "https://badeend.github.io/EnumClass/diagnostics/EC2004.html");

	public override sealed ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create([
		this.NotExhaustiveDiagnostic,
		UnreachablePatternDiagnostic,
		NoCaseImplementsInterfaceDiagnostic,
	]);

	protected void AnalyzeSwitch(SyntaxNodeAnalysisContext context, ExpressionSyntax governingExpression, SyntaxToken switchKeyword, IEnumerable<Pattern> patterns)
	{
		var enumClassType = context.SemanticModel.GetEnumClassType(governingExpression);
		if (enumClassType is null)
		{
			return;
		}

		var nullability = GetExpressionNullability(context, governingExpression);

		var cases = enumClassType.GetAllEnumCases().Select(c => new EnumCase(c)).ToList();
		var isMissingNullCheck = nullability == NullableFlowState.MaybeNull;

		foreach (var pattern in patterns)
		{
			if (pattern is Pattern.Wildcard)
			{
				if (isMissingNullCheck == false)
				{
					MaybeReportUnreachablePattern(context, cases, pattern);
				}

				isMissingNullCheck = false;

				foreach (var enumCase in cases)
				{
					enumCase.UpgradeMatch(CaseMatch.Full);
				}
			}
			else if (pattern is Pattern.Null)
			{
				isMissingNullCheck = false;
			}
			else if (pattern is Pattern.Match matchPattern && context.SemanticModel.GetSymbolInfo(matchPattern.Comparand).Symbol is INamedTypeSymbol symbol)
			{
				var matchedAnyNewCases = false;
				var matchesAnyKnownCase = false;
				foreach (var enumCase in cases)
				{
					var caseMatch = ClassifyCaseMatch(context, matchPattern, symbol, enumCase.Type);
					if (caseMatch == CaseMatch.None)
					{
						continue;
					}

					matchesAnyKnownCase = true;

					// Perform this check _before_ calling UpgradeMatch
					if (enumCase.Match != CaseMatch.Full)
					{
						matchedAnyNewCases = true;
					}

					enumCase.UpgradeMatch(caseMatch);
				}

				if (matchesAnyKnownCase)
				{
					if (!matchedAnyNewCases)
					{
						ReportDiagnostic(context, matchPattern.Comparand, UnreachablePatternDiagnostic, "This pattern has already been handled by previous matches.");
					}
				}
				else
				{
					MaybeReportUnreachablePattern(context, cases, pattern);

					if (symbol.TypeKind == TypeKind.Interface)
					{
						ReportDiagnostic(context, matchPattern.Comparand, NoCaseImplementsInterfaceDiagnostic);
					}

					// Let CS8510 ("The pattern is unreachable. ...") handle the rest.
				}
			}
			else
			{
				MaybeReportUnreachablePattern(context, cases, pattern);
			}
		}

		if (isMissingNullCheck)
		{
			ReportDiagnostic(context, switchKeyword, this.NotExhaustiveDiagnostic, "The value being switched on can be null, but none of the arms check for it");
		}

		var unmatchedCases = cases.Where(c => c.Match != CaseMatch.Full).Select(c => c.Type).ToArray();
		if (unmatchedCases.Length > 0)
		{
			var partiallyMatchedCases = cases.Where(c => c.Match == CaseMatch.Partial).Select(c => c.Type).ToArray();
			if (partiallyMatchedCases.Length == unmatchedCases.Length)
			{
				ReportDiagnostic(context, switchKeyword, this.NotExhaustiveDiagnostic, $"The following cases are being matched on, but only partially: {FormatCaseListForHumans(enumClassType, partiallyMatchedCases)}.");
			}
			else if (partiallyMatchedCases.Length > 0)
			{
				ReportDiagnostic(context, switchKeyword, this.NotExhaustiveDiagnostic, $"Unhandled cases: {FormatCaseListForHumans(enumClassType, unmatchedCases)}. Some of these are already being matched on, but only partially: {FormatCaseListForHumans(enumClassType, partiallyMatchedCases)}.");
			}
			else
			{
				ReportDiagnostic(context, switchKeyword, this.NotExhaustiveDiagnostic, $"Unhandled cases: {FormatCaseListForHumans(enumClassType, unmatchedCases)}.");
			}
		}
	}

	private enum CaseMatch
	{
		None,
		Partial,
		Full,
	}

	private static CaseMatch ClassifyCaseMatch(SyntaxNodeAnalysisContext context, Pattern.Match match, INamedTypeSymbol symbol, INamedTypeSymbol caseType)
	{
		if (IsAssignableTo(context, caseType, symbol))
		{
			return match.IsPartial == false ? CaseMatch.Full : CaseMatch.Partial;
		}

		if (IsAssignableTo(context, symbol, caseType))
		{
			return CaseMatch.Partial;
		}

		return CaseMatch.None;
	}

	private sealed record EnumCase(INamedTypeSymbol Type)
	{
		public CaseMatch Match { get; private set; } = CaseMatch.None;

		public void UpgradeMatch(CaseMatch newMatch)
		{
			if (newMatch > this.Match)
			{
				this.Match = newMatch;
			}
		}
	}

	private static void MaybeReportUnreachablePattern(SyntaxNodeAnalysisContext context, List<EnumCase> cases, Pattern pattern)
	{
		if (cases.All(c => c.Match == CaseMatch.Full))
		{
			ReportDiagnostic(context, pattern.Node, UnreachablePatternDiagnostic, "All enum cases have already been handled.");
		}
	}

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

	private static bool IsAssignableTo(SyntaxNodeAnalysisContext context, ITypeSymbol from, ITypeSymbol to)
	{
		var conversion = context.Compilation.ClassifyConversion(from, to);

		return conversion.IsIdentity || (conversion.IsReference && conversion.IsImplicit);
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

	private static NullableFlowState GetExpressionNullability(SyntaxNodeAnalysisContext context, ExpressionSyntax expression)
	{
		var flowState = context.SemanticModel.GetTypeInfo(expression).Nullability.FlowState;
		if (flowState != NullableFlowState.None)
		{
			return flowState;
		}

		// Ideally, `flowState` should have been all we needed. Sadly, Roslyn thinks otherwise...
		// See: https://github.com/dotnet/roslyn/issues/59875
		return context.SemanticModel.GetSymbolInfo(expression).Symbol switch
		{
			IParameterSymbol s => AnnotationToFlowState(s.NullableAnnotation),
			ILocalSymbol s => AnnotationToFlowState(s.NullableAnnotation),
			IPropertySymbol s => AnnotationToFlowState(s.NullableAnnotation),
			IFieldSymbol s => AnnotationToFlowState(s.NullableAnnotation),
			_ => NullableFlowState.None,
		};

		static NullableFlowState AnnotationToFlowState(NullableAnnotation annotation) => annotation switch
		{
			NullableAnnotation.NotAnnotated => NullableFlowState.NotNull,
			NullableAnnotation.Annotated => NullableFlowState.MaybeNull,
			_ => NullableFlowState.None,
		};
	}

	private static void ReportDiagnostic(SyntaxNodeAnalysisContext context, SyntaxNode node, DiagnosticDescriptor diagnostic, params string?[]? messageArgs)
	{
		ReportDiagnostic(context, node.GetLocation(), diagnostic, messageArgs);
	}

	private static void ReportDiagnostic(SyntaxNodeAnalysisContext context, SyntaxToken token, DiagnosticDescriptor diagnostic, params string?[]? messageArgs)
	{
		ReportDiagnostic(context, token.GetLocation(), diagnostic, messageArgs);
	}

	private static void ReportDiagnostic(SyntaxNodeAnalysisContext context, Location location, DiagnosticDescriptor diagnostic, params string?[]? messageArgs)
	{
		context.ReportDiagnostic(Diagnostic.Create(diagnostic, location, messageArgs));
	}

	protected abstract record Pattern(SyntaxNode Node)
	{
		/// <summary>
		/// Pattern matches _any_ value.
		/// E.g. `var abc`, `_`, `default:`
		/// .
		/// </summary>
		internal record Wildcard(SyntaxNode Node) : Pattern(Node);

		/// <summary>
		/// Pattern matches `null`.
		/// </summary>
		internal record Null(SyntaxNode Node) : Pattern(Node);

		/// <summary>
		/// Pattern matches the Comparand.
		/// The semantic model is required to decide whether this is a type or a value.
		/// </summary>
		internal record Match(SyntaxNode Node, ExpressionSyntax Comparand, bool IsPartial) : Pattern(Node);

		/// <summary>
		/// Any other kind of pattern, including:
		/// - patterns that were too complex to parse,
		/// - patterns that have additional conditions.
		/// </summary>
		internal record Other(SyntaxNode Node) : Pattern(Node);

		internal static IEnumerable<Pattern> Parse(SwitchExpressionSyntax switchExpression)
		{
			return switchExpression.Arms.SelectMany(arm => ApplyWhenClause(Parse(arm.Pattern), arm.WhenClause));
		}

		internal static IEnumerable<Pattern> Parse(SwitchStatementSyntax switchStatement)
		{
			return switchStatement.Sections.SelectMany(section => section.Labels).SelectMany(label => label switch
			{
				DefaultSwitchLabelSyntax defaultSwitchLabel => [new Wildcard(defaultSwitchLabel)],
				CaseSwitchLabelSyntax caseSwitchLabel => ParseExpression(caseSwitchLabel.Value, partial: false),
				CasePatternSwitchLabelSyntax casePatternSwitchLabel => ApplyWhenClause(Parse(casePatternSwitchLabel.Pattern), casePatternSwitchLabel.WhenClause),

				_ => [new Other(label)],
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
				Match match => match with { IsPartial = true },
				_ => (Pattern)new Other(pattern.Node),
			});
		}

		internal static IEnumerable<Pattern> Parse(PatternSyntax pattern) => pattern switch
		{
			DiscardPatternSyntax => [new Wildcard(pattern)],
			ParenthesizedPatternSyntax parenthesizedPattern => Parse(parenthesizedPattern.Pattern),
			TypePatternSyntax typePattern => ParseExpression(typePattern.Type, partial: false),
			VarPatternSyntax => [new Wildcard(pattern)],
			DeclarationPatternSyntax declarationPattern => ParseExpression(declarationPattern.Type, partial: false),
			BinaryPatternSyntax binaryPattern => ParseBinaryPattern(binaryPattern),
			ConstantPatternSyntax constantPattern => ParseExpression(constantPattern.Expression, partial: false),
			RecursivePatternSyntax recursivePattern => ParseRecursivePattern(recursivePattern),

			ListPatternSyntax or
			RelationalPatternSyntax or
			SlicePatternSyntax or
			UnaryPatternSyntax or
			_ => [new Other(pattern)],
		};

		private static IEnumerable<Pattern> ParseExpression(ExpressionSyntax expression, bool partial)
		{
			if (IsNullLiteral(expression))
			{
				return [new Null(expression)];
			}

			return [new Match(expression, expression, partial)];
		}

		private static IEnumerable<Pattern> ParseBinaryPattern(BinaryPatternSyntax binaryPattern)
		{
			if (binaryPattern.OperatorToken.IsKind(SyntaxKind.OrKeyword))
			{
				return ParseOrPattern(binaryPattern);
			}
			else if (binaryPattern.OperatorToken.IsKind(SyntaxKind.AndKeyword))
			{
				return ParseAndPattern(binaryPattern);
			}
			else
			{
				// Unknown keyword. Assume the worst case:
				return [new Other(binaryPattern)];
			}
		}

		private static IEnumerable<Pattern> ParseOrPattern(BinaryPatternSyntax orPattern)
		{
			return Parse(orPattern.Left).Concat(Parse(orPattern.Right));
		}

		private static IEnumerable<Pattern> ParseAndPattern(BinaryPatternSyntax andPattern)
		{
			if (IsWildcard(andPattern.Left) && IsWildcard(andPattern.Right))
			{
				return [new Wildcard(andPattern)];
			}

			return [new Other(andPattern)];
		}

		private static IEnumerable<Pattern> ParseRecursivePattern(RecursivePatternSyntax recursivePattern)
		{
			if (recursivePattern.Type is not null)
			{
				var positionalPatternsAreWildcard = recursivePattern.PositionalPatternClause?.Subpatterns.All(p => IsWildcard(p.Pattern)) ?? true;
				var propertyPatternsAreWildcard = recursivePattern.PropertyPatternClause?.Subpatterns.All(p => IsWildcard(p.Pattern)) ?? true;

				if (positionalPatternsAreWildcard && propertyPatternsAreWildcard)
				{
					return ParseExpression(recursivePattern.Type, partial: false);
				}
				else
				{
					return ParseExpression(recursivePattern.Type, partial: true);
				}
			}
			else
			{
				if (recursivePattern.PositionalPatternClause is null && recursivePattern.PropertyPatternClause is null)
				{
					return [new Wildcard(recursivePattern)];
				}
				else
				{
					return [new Other(recursivePattern)];
				}
			}
		}

		private static bool IsWildcard(PatternSyntax pattern)
		{
			return Parse(pattern).All(p => p is Wildcard);
		}

		private static bool IsNullLiteral(ExpressionSyntax expression)
		{
			return expression is LiteralExpressionSyntax literalExpression && literalExpression.Token.IsKind(SyntaxKind.NullKeyword);
		}
	}
}
