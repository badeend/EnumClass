using Badeend.EnumClass.Reflection;
using Badeend.EnumClass.TestAssets;

namespace Badeend.EnumClass.Tests;

public class ReflectionTests
{
	[Fact]
	public void Records()
	{
		Assert.True(typeof(RecordShape).IsEnumClass());
		Assert.False(typeof(RecordShape).IsEnumClassCase());
		Assert.Null(typeof(RecordShape).GetDeclaringEnumClass());
		var cases = typeof(RecordShape).GetEnumClassCases();
		Assert.Equal(4, cases.Length);
		Assert.Contains(typeof(RecordShape.Circle), cases);
		Assert.Contains(typeof(RecordShape.Rectangle), cases);
		Assert.Contains(typeof(RecordShape.Triangle), cases);
		Assert.Contains(typeof(RecordShape.Polygon), cases);
		Assert.DoesNotContain(typeof(RecordShape.Square), cases);

		Assert.False(typeof(RecordShape.Circle).IsEnumClass());
		Assert.True(typeof(RecordShape.Circle).IsEnumClassCase());
		Assert.Equal(typeof(RecordShape), typeof(RecordShape.Circle).GetDeclaringEnumClass());
		Assert.Empty(typeof(RecordShape.Circle).GetEnumClassCases());

		Assert.True(typeof(RecordShape.Polygon).IsEnumClass());
		Assert.True(typeof(RecordShape.Polygon).IsEnumClassCase());
		Assert.Equal(typeof(RecordShape), typeof(RecordShape.Polygon).GetDeclaringEnumClass());

		Assert.False(typeof(RecordShape.Square).IsEnumClass());
		Assert.False(typeof(RecordShape.Square).IsEnumClassCase());
		Assert.Null(typeof(RecordShape.Square).GetDeclaringEnumClass());
		Assert.Empty(typeof(RecordShape.Square).GetEnumClassCases());
	}

	[Fact]
	public void Classes()
	{
		Assert.True(typeof(ClassShape).IsEnumClass());
		Assert.False(typeof(ClassShape).IsEnumClassCase());
		Assert.Null(typeof(ClassShape).GetDeclaringEnumClass());
		var cases = typeof(ClassShape).GetEnumClassCases();
		Assert.Equal(3, cases.Length);
		Assert.Contains(typeof(ClassShape.Circle), cases);
		Assert.Contains(typeof(ClassShape.Rectangle), cases);
		Assert.Contains(typeof(ClassShape.Triangle), cases);

		Assert.False(typeof(ClassShape.Circle).IsEnumClass());
		Assert.True(typeof(ClassShape.Circle).IsEnumClassCase());
		Assert.Equal(typeof(ClassShape), typeof(ClassShape.Circle).GetDeclaringEnumClass());
		Assert.Empty(typeof(ClassShape.Circle).GetEnumClassCases());
	}

	[Fact]
	public void OpenGenerics()
	{
		Assert.True(typeof(GenericNode<>).IsEnumClass());
		Assert.False(typeof(GenericNode<>).IsEnumClassCase());
		Assert.Null(typeof(GenericNode<>).GetDeclaringEnumClass());
		var cases = typeof(GenericNode<>).GetEnumClassCases();
		Assert.Equal(2, cases.Length);
		Assert.Contains(typeof(GenericNode<>.Leaf), cases);
		Assert.Contains(typeof(GenericNode<>.Branch), cases);
		Assert.DoesNotContain(typeof(GenericNode<>.SpecialLeaf), cases);

		Assert.False(typeof(GenericNode<>.Leaf).IsEnumClass());
		Assert.True(typeof(GenericNode<>.Leaf).IsEnumClassCase());
		Assert.Equal(typeof(GenericNode<>), typeof(GenericNode<>.Leaf).GetDeclaringEnumClass());
		Assert.Empty(typeof(GenericNode<>.Leaf).GetEnumClassCases());

		Assert.False(typeof(GenericNode<>.SpecialLeaf).IsEnumClass());
		Assert.False(typeof(GenericNode<>.SpecialLeaf).IsEnumClassCase());
		Assert.Null(typeof(GenericNode<>.SpecialLeaf).GetDeclaringEnumClass());
	}

	[Fact]
	public void ConstructedGenerics()
	{
		Assert.True(typeof(GenericNode<int>).IsEnumClass());
		Assert.False(typeof(GenericNode<int>).IsEnumClassCase());
		Assert.Null(typeof(GenericNode<int>).GetDeclaringEnumClass());
		var cases = typeof(GenericNode<int>).GetEnumClassCases();
		Assert.Equal(2, cases.Length);
		Assert.Contains(typeof(GenericNode<int>.Leaf), cases);
		Assert.Contains(typeof(GenericNode<int>.Branch), cases);
		Assert.DoesNotContain(typeof(GenericNode<int>.SpecialLeaf), cases);

		Assert.False(typeof(GenericNode<int>.Leaf).IsEnumClass());
		Assert.True(typeof(GenericNode<int>.Leaf).IsEnumClassCase());
		Assert.Equal(typeof(GenericNode<int>), typeof(GenericNode<int>.Leaf).GetDeclaringEnumClass());
		Assert.Empty(typeof(GenericNode<int>.Leaf).GetEnumClassCases());

		Assert.False(typeof(GenericNode<int>.SpecialLeaf).IsEnumClass());
		Assert.False(typeof(GenericNode<int>.SpecialLeaf).IsEnumClassCase());
		Assert.Null(typeof(GenericNode<int>.SpecialLeaf).GetDeclaringEnumClass());
	}

	[Fact]
	public void NonEnum()
	{
		Assert.False(typeof(NotAnEnum).IsEnumClass());
		Assert.False(typeof(NotAnEnum).IsEnumClassCase());
		Assert.Null(typeof(NotAnEnum).GetDeclaringEnumClass());
		Assert.Empty(typeof(NotAnEnum).GetEnumClassCases());

		Assert.False(typeof(NotAnEnum.Circle).IsEnumClass());
		Assert.False(typeof(NotAnEnum.Circle).IsEnumClassCase());
		Assert.Null(typeof(NotAnEnum.Circle).GetDeclaringEnumClass());
		Assert.Empty(typeof(NotAnEnum.Circle).GetEnumClassCases());
	}
}
