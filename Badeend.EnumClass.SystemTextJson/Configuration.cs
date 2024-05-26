using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Badeend.EnumClass.SystemTextJson;

/// <summary>
/// Methods for configuring <c>System.Text.Json</c>.
/// </summary>
[SuppressMessage("Microsoft.Naming", "CA1724:TypeNamesShouldNotMatchNamespaces", Justification = "Don't care; the name `Configuration` is too generic and the type `System.Configuration` is not widespread enough for me to care.")]
public static class Configuration
{
	private static readonly IJsonTypeInfoResolver DefaultJsonTypeInfoResolver = new DefaultJsonTypeInfoResolver();

	/// <summary>
	/// Configure <c>System.Text.Json</c> to automatically serialize and deserialize subclasses of <c>[EnumClass]</c> types.
	/// </summary>
	/// <returns>The <paramref name="serializerOptions"/> instance for further chaining.</returns>
	public static JsonSerializerOptions AddEnumClasses(this JsonSerializerOptions serializerOptions, EnumClassJsonOptions? enumClassOptions = null)
	{
		if (serializerOptions is null)
		{
			throw new ArgumentNullException(nameof(serializerOptions));
		}

		enumClassOptions ??= new();

		var existingResolver = serializerOptions.TypeInfoResolver ?? DefaultJsonTypeInfoResolver;

		serializerOptions.TypeInfoResolver = existingResolver.WithAddedModifier(enumClassOptions.ModifyEnumClassTypeInfo);

		return serializerOptions;
	}
}
