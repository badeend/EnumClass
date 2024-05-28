using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;

namespace Badeend.EnumClass.CodeFixes;

[ExportCodeFixProvider(LanguageNames.CSharp)]
[Shared]
public sealed class EC1030CodeFix : BaseCodeFix
{
	public override string DiagnosticId => "EC1030";

	public override void SetUpCodeFixes(CodeFixContext context, SyntaxNode node)
	{
		if (node is TypeDeclarationSyntax declaration)
		{
			var parent = declaration.FirstAncestorOrSelf<TypeDeclarationSyntax>(d => d != declaration);
			var parentName = parent?.Identifier.Text;

			if (!string.IsNullOrWhiteSpace(parentName))
			{
				this.RegisterCodeFix(context, $"Extend {parentName}", editor => ExtendParentClass(editor, declaration, parentName!));
			}
		}
	}

	private static void ExtendParentClass(DocumentEditor editor, TypeDeclarationSyntax declaration, string parentName)
	{
		var baseType = (BaseTypeSyntax)SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName(parentName));

		var newBaseList = declaration.BaseList is not null
			? declaration.BaseList.WithTypes(declaration.BaseList.Types.Insert(0, baseType))
			: SyntaxFactory.BaseList(SyntaxFactory.SingletonSeparatedList(baseType));

		editor.ReplaceNode(declaration, declaration.WithBaseList(newBaseList));
	}
}
