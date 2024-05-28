using Microsoft.CodeAnalysis;

namespace Badeend.EnumClass.Internals;

// If only we could apply [EnumClass] here :)
internal abstract record Pattern(SyntaxNode Node)
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
	/// Pattern matches a type.
	/// </summary>
	internal record TypeCheck(SyntaxNode Node, INamedTypeSymbol Type, bool IsPartial) : Pattern(Node);

	/// <summary>
	/// Any other kind of pattern, including:
	/// - patterns that were too complex to parse,
	/// - patterns that have additional conditions.
	/// </summary>
	internal record Other(SyntaxNode Node) : Pattern(Node);
}
