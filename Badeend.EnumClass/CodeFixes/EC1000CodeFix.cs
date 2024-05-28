using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;

namespace Badeend.EnumClass.CodeFixes;

[ExportCodeFixProvider(LanguageNames.CSharp)]
[Shared]
public sealed class EC1000CodeFix : BaseCodeFix
{
	public override string DiagnosticId => "EC1000";

	public override void SetUpCodeFixes(CodeFixContext context, SyntaxNode node)
	{
		if (node is TypeDeclarationSyntax typeDeclarationNode)
		{
			this.RegisterCodeFix(context, "Make abstract", editor => AddAbstractModifier(editor, typeDeclarationNode));
		}
	}

	private static void AddAbstractModifier(DocumentEditor editor, TypeDeclarationSyntax typeDeclarationNode)
	{
		var remainingModifiers = typeDeclarationNode.Modifiers.Where(t => t.Kind() is not (
			SyntaxKind.SealedKeyword or
			SyntaxKind.AbstractKeyword));
		var newModifiers = remainingModifiers.Concat([SyntaxFactory.Token(SyntaxKind.AbstractKeyword)]);

		editor.ReplaceNode(typeDeclarationNode, typeDeclarationNode.WithModifiers(new SyntaxTokenList(newModifiers)));
	}
}
