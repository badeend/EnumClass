using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Editing;

namespace Badeend.EnumClass.CodeFixes;

public abstract class BaseCodeFix : CodeFixProvider
{
	/// <summary>
	/// The identifier string emitted by the analyzer.
	/// </summary>
	public abstract string DiagnosticId { get; }

	/// <summary>
	/// The `node` parameter is the node on which the analyzer reported the diagnostic.
	/// </summary>
	public abstract void SetUpCodeFixes(CodeFixContext context, SyntaxNode node);

	public override sealed ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(this.DiagnosticId);

	public override sealed FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

	public override sealed async Task RegisterCodeFixesAsync(CodeFixContext context)
	{
		var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
		var node = root?.FindNode(context.Span, getInnermostNodeForTie: true);
		if (node is null)
		{
			return;
		}

		this.SetUpCodeFixes(context, node);
	}

	protected void RegisterCodeFix(CodeFixContext context, string title, Action<DocumentEditor> editDocument)
	{
		var createChangedDocument = async (CancellationToken cancellationToken) =>
		{
			var editor = await DocumentEditor.CreateAsync(context.Document, cancellationToken).ConfigureAwait(false);

			editDocument(editor);

			return editor.GetChangedDocument();
		};

		context.RegisterCodeFix(CodeAction.Create(title, createChangedDocument, equivalenceKey: title), context.Diagnostics);
	}
}
