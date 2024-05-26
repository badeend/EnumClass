using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Badeend.EnumClass.Reflection;

namespace Badeend.EnumClass.SystemTextJson;

/// <summary>
/// Settings to alter the default (de)serialization behavior of enum classes.
/// </summary>
public sealed record EnumClassJsonOptions
{
	/// <summary>
	/// The policy used to automatically convert an enum case's type name to a
	/// discriminator string.
	///
	/// If set to <c>null</c>, the type name is used as-is without any alterations.
	/// (This is the default behavior.)
	/// </summary>
	/// <remarks>
	/// This setting only applies to automatically inferred discriminators.
	/// If an enum case specifies an explicit discriminator using e.g.
	/// <see cref="JsonDiscriminatorAttribute"><c>[JsonDiscriminator]</c></see> or
	/// <see cref="JsonDerivedType"><c>[JsonDerivedType]</c></see>,
	/// this setting does not apply.
	/// </remarks>
	public JsonNamingPolicy? DiscriminatorNamingPolicy { get; set; } = null;

	internal void ModifyEnumClassTypeInfo(JsonTypeInfo info)
	{
		var enumClass = info.Type;
		if (!enumClass.IsEnumClass())
		{
			return;
		}

		info.PolymorphismOptions ??= new JsonPolymorphismOptions();

		// Only auto-populate the cases if the user hasn't manually configured their [JsonDerivedType] attributes.
		if (info.PolymorphismOptions.DerivedTypes.Count == 0)
		{
			this.AddCases(enumClass, info.PolymorphismOptions.DerivedTypes);
		}
	}

	private void AddCases(Type enumClass, IList<JsonDerivedType> builder)
	{
		var cases = enumClass.GetEnumClassCases();

		foreach (var caseType in cases)
		{
			if (caseType.IsEnumClass())
			{
				this.AddCases(caseType, builder);
			}
			else
			{
				var discriminator = this.GetCaseDiscriminator(caseType);

				builder.Add(discriminator switch
				{
					null => new JsonDerivedType(caseType),
					string s => new JsonDerivedType(caseType, s),
					int i => new JsonDerivedType(caseType, i),
					_ => throw new InvalidOperationException("Invalid discriminator type."),
				});
			}
		}
	}

	private object? GetCaseDiscriminator(Type enumCase)
	{
		var jsonDiscriminatorAttribute = enumCase.GetCustomAttribute<JsonDiscriminatorAttribute>(inherit: false);
		if (jsonDiscriminatorAttribute is not null)
		{
			return jsonDiscriminatorAttribute.Discriminator;
		}

		var typeName = enumCase.Name;
		if (this.DiscriminatorNamingPolicy is null)
		{
			return typeName;
		}

		var convertedName = this.DiscriminatorNamingPolicy.ConvertName(typeName);
		if (string.IsNullOrEmpty(convertedName))
		{
			throw new InvalidOperationException("Discriminator can not be null or empty.");
		}

		return convertedName;
	}
}
