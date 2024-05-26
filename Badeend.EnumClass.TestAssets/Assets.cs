using System.Diagnostics.CodeAnalysis;
using Badeend;
using Badeend.EnumClass;

namespace Badeend.EnumClass.TestAssets;

[EnumClass]
public abstract record RecordShape
{
	private RecordShape() {}

	public record Circle(float Radius) : RecordShape;
	public record Rectangle(float Width, float Height) : RecordShape;
	public record Triangle(float SideLength) : RecordShape;

	[EnumClass]
	public abstract record Polygon : RecordShape
	{
		private Polygon() {}

		public record Regular(int VertexCount, float SideLength) : Polygon;
		public record General(List<(float X, float Y)> Points) : Polygon;
	}


	[SuppressMessage("Declaration", "EC1030:Nested type does not extend the enum class it is part of")]
	public record Square(float Size) : RecordShape.Rectangle(Size, Size);
}

[EnumClass]
public abstract class ClassShape
{
	private ClassShape() { }

	public class Circle : ClassShape;
	public class Rectangle : ClassShape;
	public class Triangle : ClassShape;
}

[EnumClass]
public abstract record GenericNode<T>
{
	private GenericNode() {}

	public record Leaf(T Value) : GenericNode<T>;
	public record Branch(GenericNode<T> Left, GenericNode<T> Right) : GenericNode<T>;

	[SuppressMessage("Declaration", "EC1030:Nested type does not extend the enum class it is part of")]
	public record SpecialLeaf() : Leaf(default(T)!);
}

public abstract class NotAnEnum
{
	private NotAnEnum() { }

	public class Circle : NotAnEnum;
	public class Rectangle : NotAnEnum;
	public class Triangle : NotAnEnum;
}
