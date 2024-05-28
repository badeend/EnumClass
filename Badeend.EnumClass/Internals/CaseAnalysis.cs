using Microsoft.CodeAnalysis;

namespace Badeend.EnumClass.Internals;

internal sealed record CaseAnalysis
{
	internal required bool IsMissingNullCheck { get; init; }

	internal required INamedTypeSymbol EnumClassType { get; init; }

	internal required IReadOnlyList<Case> Cases { get; init; }

	internal enum CaseMatchState
	{
		None,
		Partial,
		Full,
	}

	internal sealed record Case
	{
		internal required INamedTypeSymbol Type { get; init; }

		internal CaseMatchState Match { get; private set; } = CaseMatchState.None;

		internal void UpgradeMatch(CaseMatchState newMatch)
		{
			if (newMatch > this.Match)
			{
				this.Match = newMatch;
			}
		}
	}
}
