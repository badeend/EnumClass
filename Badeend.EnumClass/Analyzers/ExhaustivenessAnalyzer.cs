using System.Collections.Immutable;
using Badeend.EnumClass.Internals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Badeend.EnumClass.Analyzers;

public abstract class ExhaustivenessAnalyzer : DiagnosticAnalyzer
{
	protected abstract DiagnosticDescriptor NotExhaustiveDiagnostic { get; }

	public override sealed ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create([
		this.NotExhaustiveDiagnostic,
		Diagnostics.EC2003_UnreachablePattern,
		Diagnostics.EC2004_NoCaseImplementsInterface,
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
			else if (pattern is Pattern.TypeCheck typeCheck)
			{
				var matchedAnyNewCases = false;
				var matchesAnyKnownCase = false;
				foreach (var enumCase in cases)
				{
					var caseMatch = ClassifyCaseMatch(context, typeCheck, enumCase.Type);
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
						ReportDiagnostic(context, typeCheck.Node, Diagnostics.EC2003_UnreachablePattern, "This pattern has already been handled by previous matches");
					}
				}
				else
				{
					MaybeReportUnreachablePattern(context, cases, pattern);

					if (typeCheck.Type.TypeKind == TypeKind.Interface)
					{
						ReportDiagnostic(context, typeCheck.Node, Diagnostics.EC2004_NoCaseImplementsInterface);
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
				ReportDiagnostic(context, switchKeyword, this.NotExhaustiveDiagnostic, $"The following cases are being matched on, but only partially: {FormatCaseListForHumans(enumClassType, partiallyMatchedCases)}");
			}
			else if (partiallyMatchedCases.Length > 0)
			{
				ReportDiagnostic(context, switchKeyword, this.NotExhaustiveDiagnostic, $"Unhandled cases: {FormatCaseListForHumans(enumClassType, unmatchedCases)}. Some of these are already being matched on, but only partially: {FormatCaseListForHumans(enumClassType, partiallyMatchedCases)}");
			}
			else
			{
				ReportDiagnostic(context, switchKeyword, this.NotExhaustiveDiagnostic, $"Unhandled cases: {FormatCaseListForHumans(enumClassType, unmatchedCases)}");
			}
		}
	}

	private enum CaseMatch
	{
		None,
		Partial,
		Full,
	}

	private static CaseMatch ClassifyCaseMatch(SyntaxNodeAnalysisContext context, Pattern.TypeCheck typeCheck, INamedTypeSymbol caseType)
	{
		if (IsAssignableTo(context, caseType, typeCheck.Type))
		{
			return typeCheck.IsPartial == false ? CaseMatch.Full : CaseMatch.Partial;
		}

		if (IsAssignableTo(context, typeCheck.Type, caseType))
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
			ReportDiagnostic(context, pattern.Node, Diagnostics.EC2003_UnreachablePattern, "All enum cases have already been handled");
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
}
