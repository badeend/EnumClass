# JSON

## System.Text.Json

This package builds on top of `System.Text.Json`s [built-in support for polymorphism](https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/polymorphism). Enabling this package automatically marks every enum class as [`[JsonPolymorphic]`](https://learn.microsoft.com/en-us/dotnet/api/system.text.json.serialization.jsonpolymorphicattribute) and adds every enum case as a [`[JsonDerivedType]`](https://learn.microsoft.com/en-us/dotnet/api/system.text.json.serialization.jsonderivedtypeattribute).

#### Installation

[![NuGet Badeend.EnumClass.SystemTextJson](https://img.shields.io/nuget/v/Badeend.EnumClass.SystemTextJson?label=Badeend.EnumClass.SystemTextJson)](https://www.nuget.org/packages/Badeend.EnumClass.SystemTextJson)

```sh
dotnet add package Badeend.EnumClass.SystemTextJson
```

#### Configure standalone `JsonSerializerOptions`

```cs
using Badeend.EnumClass.SystemTextJson;

var options = new JsonSerializerOptions();

options.AddEnumClasses(); // <--- HERE

var _ = JsonSerializer.Serialize(myObj, options);

```

#### Configure ASP.NET Core

Depending on which parts of ASP.NET Core you use, you might need to configure it twice:

```cs
using Badeend.EnumClass.SystemTextJson;

builder.Services.Configure<Microsoft.AspNetCore.Mvc.JsonOptions>(options =>
{
    options.JsonSerializerOptions.AddEnumClasses(); // <--- HERE
});

// And/or:

builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(options =>
{
    options.SerializerOptions.AddEnumClasses(); // <--- HERE
});
```

### Customization

#### Discriminator values
Given the following enum:

```cs
[EnumClass]
public abstract record Shape
{
    public record BigCircle(float Radius) : Shape;
}
```

###### Default
By default, the _type names_ of the enum cases are used as discriminator values:

```json
{
    "$type": "BigCircle",
    "Radius": 3.14
}
```

###### DiscriminatorNamingPolicy
When calling `.AddEnumClasses()` you can optionally pass any [`JsonNamingPolicy`](https://learn.microsoft.com/en-us/dotnet/api/system.text.json.jsonnamingpolicy) to alter the default naming scheme. For example:

```cs
options.AddEnumClasses(new() { DiscriminatorNamingPolicy = JsonNamingPolicy.KebabCaseLower });
```

```json
{
    "$type": "big-circle",
    "Radius": 3.14
}
```

###### \[JsonDiscriminator\]
Alternatively, you can override the defaults by annotating the discriminator on a case-by-case basis:

```cs
using Badeend.EnumClass.SystemTextJson;

[EnumClass]
public abstract record Shape
{
    [JsonDiscriminator("ROUND")]
    public record BigCircle(float Radius) : Shape;
}
```

```json
{
    "$type": "ROUND",
    "Radius": 3.14
}
```

#### Discriminator property name
By default, `System.Text.Json` uses `$type` as the discriminator property name. You can still continue to use the `[JsonPolymorphic]` attribute to configure this (and other) settings. For example:

```cs
[EnumClass]
[JsonPolymorphic(TypeDiscriminatorPropertyName = "my-type")]
public abstract class MyEnum
{
    // ...
}
```

#### Complete control
If an enum class declares any `[JsonDerivedType]` attribute, this package backs off completely and won't perform _any_ automatic registration. It is then back up to you to register the subtypes you want to be serializable:

```cs
[EnumClass]
[JsonPolymorphic]
[JsonDerivedType(typeof(Shape.BigCircle), "Circle")]
public abstract record Shape
{
    public record Circle(float Radius) : Shape;

    public record SecretRectangle(float Width, float Height) : Shape;
}
```
