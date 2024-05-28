using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;

namespace Badeend.EnumClass.CodeFixes;

[ExportCodeFixProvider(LanguageNames.CSharp)]
[Shared]
public sealed class EC1005CodeFix : BaseCodeFix
{
	public override string DiagnosticId => Diagnostics.EC1005;

	public override void SetUpCodeFixes(CodeFixContext context, SyntaxNode node)
	{
		if (node is ConstructorDeclarationSyntax declaration)
		{
			this.RegisterCodeFix(context, "Make private", editor => MakePublic(editor, declaration));
		}
	}

	private static void MakePublic(DocumentEditor editor, ConstructorDeclarationSyntax declaration)
	{
		var remainingModifiers = declaration.Modifiers.Where(t => t.Kind() is not (SyntaxKind.PrivateKeyword or SyntaxKind.InternalKeyword or SyntaxKind.ProtectedKeyword or SyntaxKind.PublicKeyword or SyntaxKind.FileKeyword));
		var newModifiers = new[] { SyntaxFactory.Token(SyntaxKind.PrivateKeyword) }.Concat(remainingModifiers);
		editor.ReplaceNode(declaration, declaration.WithModifiers(new SyntaxTokenList(newModifiers)));
	}
}
