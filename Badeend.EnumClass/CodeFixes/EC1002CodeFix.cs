using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;

namespace Badeend.EnumClass.CodeFixes;

[ExportCodeFixProvider(LanguageNames.CSharp)]
[Shared]
public sealed class EC1002CodeFix : BaseCodeFix
{
	public override string DiagnosticId => "EC1002";

	public override void SetUpCodeFixes(CodeFixContext context, SyntaxNode node)
	{
		if (node is TypeDeclarationSyntax typeDeclarationNode)
		{
			this.RegisterCodeFix(context, "Make public", editor => MakePublic(editor, typeDeclarationNode));
		}
	}

	private static void MakePublic(DocumentEditor editor, TypeDeclarationSyntax typeDeclarationNode)
	{
		var remainingModifiers = typeDeclarationNode.Modifiers.Where(t => t.Kind() is not (
			SyntaxKind.PrivateKeyword or
			SyntaxKind.InternalKeyword or
			SyntaxKind.ProtectedKeyword or
			SyntaxKind.PublicKeyword or
			SyntaxKind.FileKeyword));
		var newModifiers = new[] { SyntaxFactory.Token(SyntaxKind.PublicKeyword) }.Concat(remainingModifiers);
		editor.ReplaceNode(typeDeclarationNode, typeDeclarationNode.WithModifiers(new SyntaxTokenList(newModifiers)));
	}
}
