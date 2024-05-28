# Reflection

Enum classes & cases can be inspected at runtime using the auxiliary `Badeend.EnumClass.Reflection` package:

[![NuGet Badeend.EnumClass.Reflection](https://img.shields.io/nuget/v/Badeend.EnumClass.Reflection?label=Badeend.EnumClass.Reflection)](https://www.nuget.org/packages/Badeend.EnumClass.Reflection)

```sh
dotnet add package Badeend.EnumClass.Reflection
```

[**API documentation →**](xref:Badeend.EnumClass.Reflection.TypeExtensions)

### Quick overview

```cs
typeof(Shape).IsEnumClass();                   // => true
typeof(Shape.Circle).IsEnumClass();            // => false
typeof(Shape.Polygon).IsEnumClass();           // => true
typeof(NotAnEnum).IsEnumClass();               // => false

typeof(Shape).IsEnumClassCase();               // => false
typeof(Shape.Circle).IsEnumClassCase();        // => true
typeof(Shape.Polygon).IsEnumClassCase();       // => true
typeof(NotAnEnum).IsEnumClassCase();           // => false

typeof(Shape).GetDeclaringEnumClass();         // => null
typeof(Shape.Circle).GetDeclaringEnumClass();  // => typeof(Shape)
typeof(Shape.Polygon).GetDeclaringEnumClass(); // => typeof(Shape)
typeof(NotAnEnum).GetDeclaringEnumClass();     // => null

typeof(Shape).GetEnumClassCases();             // => [typeof(Shape.Circle), typeof(Shape.Rectangle), typeof(Shape.Polygon)]
typeof(Shape.Circle).GetEnumClassCases();      // => []
typeof(Shape.Polygon).GetEnumClassCases();     // => [typeof(Shape.Polygon.Hexagon), typeof(Shape.Polygon.Bestagon)]
typeof(NotAnEnum).GetEnumClassCases();         // => []





[EnumClass]
public abstract record Shape
{
    private Shape() {}

    public record Circle(double Radius) : Shape;
    public record Rectangle(double Width, double Height) : Shape;
    public record Square(double Size) : Rectangle(Size, Size); // Not an enum case

    [EnumClass]
    public abstract record Polygon : Shape
    {
        private Polygon() {}

        public record Hexagon(float SideLength) : Polygon;
        public record Bestagon(float SideLength) : Polygon;
    }
}

public class NotAnEnum
{
}
```

> [!NOTE]
> This reflection package assumes that when a type has been marked as [EnumClass], it fully satisfies all the requirements that are normally checked by the analyzers in the primary nuget package. Usage of the [EnumClass] attribute without running the analyzers is not supported and might trigger undefined behavior in the reflection package.
