using System.Reflection;

namespace Badeend.EnumClass.Reflection;

/// <summary>
/// Utilities for inspecting enum class types at runtime.
/// </summary>
/// <remarks>
/// These methods assume that when a type has been marked as <c>[EnumClass]</c>,
/// it fully satisfies all the requirements that are normally checked by the
/// analyzers in the primary nuget package. Usage of the <c>[EnumClass]</c>
/// attribute without running the analyzers is not supported and might trigger
/// undefined behavior.
/// </remarks>
public static class TypeExtensions
{
	private const string EnumAttributeName = "Badeend.EnumClassAttribute";

	/// <summary>
	/// Check to see if the specified <paramref name="type"/> is an "enum class".
	///
	/// Only the provided type itself is checked, it does not search up the
	/// inheritance chain.
	/// </summary>
	public static bool IsEnumClass(this Type type)
	{
		// Attempt to bail fast, without having to search through the attributes.
		if (type.IsClass == false || type.IsAbstract == false)
		{
			return false;
		}

		foreach (var attribute in type.CustomAttributes)
		{
			if (attribute.AttributeType.FullName == EnumAttributeName)
			{
				return true;
			}
		}

		return false;
	}

	/// <summary>
	/// Check to see if the specified <paramref name="type"/> is a "case" within
	/// an enum class.
	///
	/// Only the provided type itself is checked, it does not search up the
	/// inheritance chain.
	/// </summary>
	public static bool IsEnumClassCase(this Type type)
	{
		return type.GetDeclaringEnumClass() is not null;
	}

	/// <summary>
	/// Returns the enum class this case is part of.
	///
	/// Returns <c>null</c> if the provided <paramref name="caseType"/> is not
	/// part of any enum class.
	/// </summary>
	public static Type? GetDeclaringEnumClass(this Type caseType)
	{
		var baseType = caseType.BaseType;
		if (baseType is null)
		{
			return null;
		}

		if (!baseType.IsEnumClass())
		{
			return null;
		}

		if (caseType.IsGenericTypeDefinition)
		{
			return baseType.GetGenericTypeDefinition();
		}
		else
		{
			return baseType;
		}
	}

	/// <summary>
	/// Get the types of all the cases in no particular order.
	///
	/// Returns an empty array if <paramref name="enumType"/> is not an enum class.
	/// </summary>
	public static Type[] GetEnumClassCases(this Type enumType)
	{
		if (!enumType.IsEnumClass())
		{
			return Array.Empty<Type>();
		}

		var nestedTypes = enumType.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic);
		var cases = new List<Type>(capacity: nestedTypes.Length);

		if (enumType.IsGenericType)
		{
			if (enumType.IsGenericTypeDefinition)
			{
				GetEnumClassCases_OpenGeneric(enumType, nestedTypes, cases);
			}
			else
			{
				GetEnumClassCases_ConstructedGeneric(enumType, nestedTypes, cases);
			}
		}
		else
		{
			GetEnumClassCases_NonGeneric(enumType, nestedTypes, cases);
		}

		return cases.ToArray();
	}

	private static void GetEnumClassCases_NonGeneric(Type enumType, Type[] nestedTypes, List<Type> builder)
	{
		foreach (var nestedType in nestedTypes)
		{
			if (nestedType.BaseType == enumType)
			{
				builder.Add(nestedType);
			}
		}
	}

	private static void GetEnumClassCases_OpenGeneric(Type enumType, Type[] nestedTypes, List<Type> builder)
	{
		var genericEnumType = enumType.GetGenericTypeDefinition();

		foreach (var nestedType in nestedTypes)
		{
			var genericBaseType = nestedType.BaseType?.GetGenericTypeDefinition();
			if (genericBaseType == genericEnumType)
			{
				builder.Add(nestedType.GetGenericTypeDefinition());
			}
		}
	}

	private static void GetEnumClassCases_ConstructedGeneric(Type enumType, Type[] nestedTypes, List<Type> builder)
	{
		var typeArguments = enumType.GenericTypeArguments;
		var genericEnumType = enumType.GetGenericTypeDefinition();

		foreach (var nestedType in nestedTypes)
		{
			var genericBaseType = nestedType.BaseType?.GetGenericTypeDefinition();
			if (genericBaseType == genericEnumType)
			{
				builder.Add(nestedType.GetGenericTypeDefinition().MakeGenericType(typeArguments));
			}
		}
	}
}
