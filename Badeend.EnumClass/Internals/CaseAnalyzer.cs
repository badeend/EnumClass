using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Badeend.EnumClass.Internals.CaseAnalysis;

namespace Badeend.EnumClass.Internals;

internal sealed class CaseAnalyzer(
	Compilation compilation,
	SemanticModel semanticModel,
	Action<Location> reportNoCaseImplementsInterface,
	Action<Location, string> reportUnreachablePattern)
{
	internal CaseAnalysis? Analyze(SwitchExpressionSyntax switchExpression)
	{
		var patterns = new PatternParser(semanticModel).Parse(switchExpression);

		return this.AnalyzeSwitch(switchExpression.GoverningExpression, patterns);
	}

	internal CaseAnalysis? Analyze(SwitchStatementSyntax switchStatement)
	{
		var patterns = new PatternParser(semanticModel).Parse(switchStatement);

		return this.AnalyzeSwitch(switchStatement.Expression, patterns);
	}

	private CaseAnalysis? AnalyzeSwitch(ExpressionSyntax governingExpression, IEnumerable<Pattern> patterns)
	{
		var enumClassType = semanticModel.GetEnumClassType(governingExpression);
		if (enumClassType is null)
		{
			return null;
		}

		var nullability = this.GetExpressionNullability(governingExpression);

		var cases = enumClassType.GetAllEnumCases().Select(c => new Case { Type = c }).ToList();
		var isMissingNullCheck = nullability == NullableFlowState.MaybeNull;

		foreach (var pattern in patterns)
		{
			if (pattern is Pattern.Wildcard)
			{
				if (isMissingNullCheck == false)
				{
					this.MaybeReportUnreachablePattern(cases, pattern);
				}

				isMissingNullCheck = false;

				foreach (var enumCase in cases)
				{
					enumCase.UpgradeMatch(CaseMatchState.Full);
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
					var caseMatch = this.ClassifyCaseMatchState(typeCheck, enumCase.Type);
					if (caseMatch == CaseMatchState.None)
					{
						continue;
					}

					matchesAnyKnownCase = true;

					// Perform this check _before_ calling UpgradeMatch
					if (enumCase.Match != CaseMatchState.Full)
					{
						matchedAnyNewCases = true;
					}

					enumCase.UpgradeMatch(caseMatch);
				}

				if (matchesAnyKnownCase)
				{
					if (!matchedAnyNewCases)
					{
						reportUnreachablePattern(typeCheck.Node.GetLocation(), "This pattern has already been handled by previous matches");
					}
				}
				else
				{
					this.MaybeReportUnreachablePattern(cases, pattern);

					if (typeCheck.Type.TypeKind == TypeKind.Interface)
					{
						reportNoCaseImplementsInterface(typeCheck.Node.GetLocation());
					}

					// Let CS8510 ("The pattern is unreachable. ...") handle the rest.
				}
			}
			else
			{
				this.MaybeReportUnreachablePattern(cases, pattern);
			}
		}

		return new CaseAnalysis
		{
			EnumClassType = enumClassType,
			Cases = cases,
			IsMissingNullCheck = isMissingNullCheck,
		};
	}

	private CaseMatchState ClassifyCaseMatchState(Pattern.TypeCheck typeCheck, INamedTypeSymbol caseType)
	{
		if (this.IsAssignableTo(caseType, typeCheck.Type))
		{
			return typeCheck.IsPartial == false ? CaseMatchState.Full : CaseMatchState.Partial;
		}

		if (this.IsAssignableTo(typeCheck.Type, caseType))
		{
			return CaseMatchState.Partial;
		}

		return CaseMatchState.None;
	}

	private void MaybeReportUnreachablePattern(List<Case> cases, Pattern pattern)
	{
		if (cases.All(c => c.Match == CaseMatchState.Full))
		{
			reportUnreachablePattern(pattern.Node.GetLocation(), "All enum cases have already been handled");
		}
	}

	private bool IsAssignableTo(ITypeSymbol from, ITypeSymbol to)
	{
		var conversion = compilation.ClassifyConversion(from, to);

		return conversion.IsIdentity || (conversion.IsReference && conversion.IsImplicit);
	}

	private NullableFlowState GetExpressionNullability(ExpressionSyntax expression)
	{
		var flowState = semanticModel.GetTypeInfo(expression).Nullability.FlowState;
		if (flowState != NullableFlowState.None)
		{
			return flowState;
		}

		// Ideally, `flowState` should have been all we needed. Sadly, Roslyn thinks otherwise...
		// See: https://github.com/dotnet/roslyn/issues/59875
		return semanticModel.GetSymbolInfo(expression).Symbol switch
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
}
