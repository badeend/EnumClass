using Microsoft.CodeAnalysis;

namespace Badeend.EnumClass.Analyzers;

[Generator]
public class AttributeSourceGenerator : ISourceGenerator
{
	public void Execute(GeneratorExecutionContext context)
	{
		var content = ReadEmbeddedResource("Badeend.EnumClass.Resources.Badeend.EnumClassAttribute.cs");
		context.AddSource($"Badeend.EnumClassAttribute.g.cs", content);
	}

	public void Initialize(GeneratorInitializationContext context)
	{
		// No initialization required for this one
	}

	private static string ReadEmbeddedResource(string resourceName)
	{
		using var stream = typeof(AttributeSourceGenerator).Assembly.GetManifestResourceStream(resourceName);
		using var reader = new StreamReader(stream);

		return reader.ReadToEnd();
	}
}
