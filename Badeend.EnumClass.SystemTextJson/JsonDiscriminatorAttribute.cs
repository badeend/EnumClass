namespace Badeend.EnumClass.SystemTextJson;

/// <summary>
/// Configures a custom discriminator value for this specific enum case.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class JsonDiscriminatorAttribute : Attribute
{
	/// <summary>
	/// The identifier to be used for the serialization of the subtype.
	/// </summary>
	public object? Discriminator { get; }

	/// <summary>
	/// Configures a custom discriminator value for this specific enum case.
	/// </summary>
	/// <exception cref="ArgumentNullException">
	/// The discriminator may not be null.
	/// </exception>
	public JsonDiscriminatorAttribute(string discriminator)
	{
		this.Discriminator = discriminator ?? throw new ArgumentNullException(nameof(discriminator));
	}

	/// <summary>
	/// Configures a custom discriminator value for this specific enum case.
	/// </summary>
	public JsonDiscriminatorAttribute(int discriminator)
	{
		this.Discriminator = discriminator;
	}
}
