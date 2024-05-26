using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using Badeend.EnumClass.SystemTextJson;

namespace Badeend.EnumClass.Tests;

public class SerializationTests
{
	[EnumClass]
	[JsonPolymorphic]
	public abstract record A<T>
	{
		private A() { }

		public record Alpha(T Value) : A<T>;

		[JsonDiscriminator("bravissimo")]
		public record Bravo(A<T> Left, A<T> Right) : A<T>;
		
		[EnumClass]
		[JsonPolymorphic]
		public abstract record Charlie : A<T>
		{
			private Charlie() {}

			public record DeltaEcho(T Value) : Charlie;

			[JsonDiscriminator("Dance!")]
			public record Foxtrot(T Value) : Charlie;
		}
	}

	[EnumClass]
	public abstract record WithoutJsonPolymorphic
	{
		private WithoutJsonPolymorphic() { }

		public record Alpha(int SomeValue) : WithoutJsonPolymorphic;
	}

	[EnumClass]
	[JsonPolymorphic]
	[JsonDerivedType(typeof(ManualDerivedTypes.Alpha), "a-l-p-h-a")]
	public abstract record ManualDerivedTypes
	{
		private ManualDerivedTypes() { }

		public record Alpha(int SomeValue) : ManualDerivedTypes;
		public record Bravo(int SomeValue) : ManualDerivedTypes;
	}

	private static readonly string DefaultsJson = Json("""
	{
	  "$type": "bravissimo",
	  "Left": {
	    "$type": "Alpha",
	    "Value": 3
	  },
	  "Right": {
	    "$type": "bravissimo",
	    "Left": {
	      "$type": "DeltaEcho",
	      "Value": 4
	    },
	    "Right": {
	      "$type": "Dance!",
	      "Value": 5
	    }
	  }
	}
	""");

	[Fact]
	public void Defaults()
	{
		var a = new A<int>.Bravo(
			Left: new A<int>.Alpha(3),
			Right: new A<int>.Bravo(
				Left: new A<int>.Charlie.DeltaEcho(4),
				Right: new A<int>.Charlie.Foxtrot(5)
			)
		);

		Assert.Equal(DefaultsJson, Serialize<A<int>>(a));
		Assert.Equal(Deserialize<A<int>>(DefaultsJson), a);
	}

	private static readonly string CustomNamingPolicyJson = Json("""
	{
	  "$type": "bravissimo",
	  "Left": {
	    "$type": "alpha",
	    "Value": 3
	  },
	  "Right": {
	    "$type": "bravissimo",
	    "Left": {
	      "$type": "delta_echo",
	      "Value": 4
	    },
	    "Right": {
	      "$type": "Dance!",
	      "Value": 5
	    }
	  }
	}
	""");

	[Fact]
	public void CustomNamingPolicy()
	{
		var a = new A<int>.Bravo(
			Left: new A<int>.Alpha(3),
			Right: new A<int>.Bravo(
				Left: new A<int>.Charlie.DeltaEcho(4),
				Right: new A<int>.Charlie.Foxtrot(5)
			)
		);

		var options = new EnumClassJsonOptions
		{
			DiscriminatorNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
		};

		Assert.Equal(CustomNamingPolicyJson, Serialize<A<int>>(a, options));
		Assert.Equal(Deserialize<A<int>>(CustomNamingPolicyJson, options), a);
	}

	private static readonly string WithoutJsonPolymorphicAttributeJson = Json("""
	{
	  "$type": "Alpha",
	  "SomeValue": 123
	}
	""");

	[Fact]
	public void WithoutJsonPolymorphicAttribute()
	{
		var a = new WithoutJsonPolymorphic.Alpha(123);

		Assert.Equal(WithoutJsonPolymorphicAttributeJson, Serialize<WithoutJsonPolymorphic>(a));
		Assert.Equal(Deserialize<WithoutJsonPolymorphic>(WithoutJsonPolymorphicAttributeJson), a);
	}

	private static readonly string ManualDerivedTypeAttributesJson = Json("""
	{
	  "$type": "a-l-p-h-a",
	  "SomeValue": 123
	}
	""");

	[Fact]
	public void ManualDerivedTypeAttributes()
	{
		var a = new ManualDerivedTypes.Alpha(123);
		var b = new ManualDerivedTypes.Bravo(123);

		Assert.Equal(ManualDerivedTypeAttributesJson, Serialize<ManualDerivedTypes>(a));
		Assert.Equal(Deserialize<ManualDerivedTypes>(ManualDerivedTypeAttributesJson), a);

		var exception = Assert.Throws<NotSupportedException>(() => Serialize<ManualDerivedTypes>(b));
		Assert.Contains("ManualDerivedTypes+Bravo' is not supported by polymorphic type", exception.Message);
	}






	private static System.Text.Json.JsonSerializerOptions CreateOptions(EnumClassJsonOptions? enumClassOptions)
	{
		var options = new System.Text.Json.JsonSerializerOptions();
		options.WriteIndented = true;
		options.AddEnumClasses(enumClassOptions);
		return options;
	}

	protected static string Serialize<T>(T obj, EnumClassJsonOptions? enumClassOptions = null)
		=> System.Text.Json.JsonSerializer.Serialize(obj, CreateOptions(enumClassOptions));

	protected static T Deserialize<T>(string json, EnumClassJsonOptions? enumClassOptions = null)
		=> System.Text.Json.JsonSerializer.Deserialize<T>(json, CreateOptions(enumClassOptions))!;

	protected static string Json([StringSyntax(StringSyntaxAttribute.Json)] string json) => json
		.Replace("\r\n", System.Environment.NewLine)
		.Replace("\n", System.Environment.NewLine);

}
