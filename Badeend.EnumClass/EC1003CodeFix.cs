using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;

namespace Badeend.EnumClass.Analyzers;

[ExportCodeFixProvider(LanguageNames.CSharp)]
[Shared]
public sealed class EC1003CodeFix : BaseCodeFix
{
	public override string DiagnosticId => "EC1003";

	public override void SetUpCodeFixes(CodeFixContext context, SyntaxNode node)
	{
		if (node is TypeDeclarationSyntax declaration)
		{
			this.RegisterCodeFix(context, "Add private constructor", editor => AddPrivateConstructor(editor, declaration));
		}
	}

	private static void AddPrivateConstructor(DocumentEditor editor, TypeDeclarationSyntax declaration)
	{
		var typeName = declaration.Identifier.Text;

		var template = $$"""
		private {{typeName}}()
		{
			// Private constructor to prevent external extension.
		}
		""";

		var constructor = SyntaxFactory.ParseMemberDeclaration(template)!
			.WithTrailingTrivia(SyntaxFactory.EndOfLine("\n\n"))
			.WithAdditionalAnnotations(Formatter.Annotation);

		editor.ReplaceNode(declaration, declaration.WithMembers(new([constructor, .. declaration.Members])));
	}
}
