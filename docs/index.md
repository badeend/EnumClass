<p align="center">
  <img src="./images/logo.png" alt="EnumClass" width="300"/>
</p>

# Introduction

This is an analyzer-only package that aims to provide a dead simple, yet complete, "discriminated unions" experience for C# with compile-time exhaustiveness checking.

"Enum classes" are a generalization of C#'s native `enums`. They can be used to represent a fixed, predefined set of possible values. Unlike regular `enum`s, "enum classes" can also store additional data per variant.

It is loosely based on the [C# proposal](https://github.com/dotnet/csharplang/blob/main/proposals/discriminated-unions.md). The proposed future syntax:
```cs
public enum class Shape
{
    Circle(float Radius),
    Rectangle(float Width, float Height),
    Triangle(float SideLength),
}
```

Unfortunately, since we don't live in the future, this is the actual syntax we'll be working with today:
```cs
using Badeend;

[EnumClass]
public abstract record Shape
{
    private Shape() {}

    public record Circle(float Radius) : Shape;
    public record Rectangle(float Width, float Height) : Shape;
    public record Triangle(float SideLength) : Shape;
}
```

A bit more verbose, but close enough... ;) FYI, this package comes bundled with automatic code fixers to help write some of this boilerplate for you.

## Installation

[![NuGet Badeend.EnumClass](https://img.shields.io/nuget/v/Badeend.EnumClass?label=Badeend.EnumClass)](https://www.nuget.org/packages/Badeend.EnumClass) [![NuGet Badeend.EnumClass.Reflection](https://img.shields.io/nuget/v/Badeend.EnumClass.Reflection?label=Badeend.EnumClass.Reflection)](https://www.nuget.org/packages/Badeend.EnumClass.Reflection) [![NuGet Badeend.EnumClass.SystemTextJson](https://img.shields.io/nuget/v/Badeend.EnumClass.SystemTextJson?label=Badeend.EnumClass.SystemTextJson)](https://www.nuget.org/packages/Badeend.EnumClass.SystemTextJson)

```sh
dotnet add package Badeend.EnumClass

# Optional:
dotnet add package Badeend.EnumClass.Reflection
dotnet add package Badeend.EnumClass.SystemTextJson
```

## More introduction

All the magic happens at compile-time as part of the analyzers shipped with this package.

Continuing with the example from above:
- We define a `Shape` "enum" type, that has three "case" types: `Circle`, `Rectangle` & `Triangle`.
- The `Shape` type has an `[EnumClass]` attribute, which is the cue for the analyzers to kick in.
- The analyzers enforce that the base type and nested subtypes satisfy all the required criteria for them to be worthy of the title "enum class". Some of these criteria can be seen right there in the example: `abstract` base type, private constructor, cases extend their parent type, etc... All for the ultimate goal:
- `Shape` is now protected against external extension and we can be sure that any `Shape` instance we encounter at runtime will be either a `Circle`, a `Rectangle` or a `Triangle`. Exactly one of those three and _nothing else_.

## Exhaustiveness checking

This is the true superpower of enum classes: all the subtypes are known at compile-time, so we can enforce that every `switch`-expression/statement on them is **exhaustive**. I.e. we can warn developers when they've missed a case:

```cs
var area = shape switch // Warning EC2001: Switch is not exhaustive. Unhandled cases: Triangle.
{
    Shape.Circle circle => Math.PI * circle.Radius * circle.Radius,
    Shape.Rectangle rectangle => rectangle.Width * rectangle.Height,
};
```

The analyzer warns us that we've not handled triangles yet. To save us some typing, it provides an `Add remaining cases` codefix that automatically appends the unhandled cases at the end of the `switch`:

```diff
  var area = shape switch
  {
      Shape.Circle circle => Math.PI * circle.Radius * circle.Radius,
      Shape.Rectangle rectangle => rectangle.Width * rectangle.Height,
+     Shape.Triangle triangle => ,
  };
```

Ofcourse it is still up to us to actually define how to compute the area of a triangle.

---

At this point, we've successfully prevented the program from blowing up, and turned a runtime error into a compile-time error.

Yay!

## Codefixes FTW

At the bare minimum you need to write the following code yourself:

```cs
[EnumClass]
record Shape
{
    record Circle(float Radius);
    record Rectangle(float Width, float Height);
    record Triangle(float SideLength);
}

var area = shape switch
{
};
```

... and can then use the codefixes to autocomplete yourself into this:

```cs
[EnumClass]
abstract record Shape
{
    private Shape()
    {
        // Private constructor to prevent external extension.
    }

    public record Circle(float Radius) : Shape;
    public record Rectangle(float Width, float Height) : Shape;
    public record Triangle(float SideLength) : Shape;
}

var area = shape switch
{
    Shape.Circle circle => ,
    Shape.Rectangle rectangle => ,
    Shape.Triangle triangle => ,
};
```

Applied fixes:
- On enum class: `Make abstract`
- On enum class: `Add private constructor`
- On enum cases: `Extend Shape`
- On enum cases: `Make public`
- On switch expression: `Add remaining cases`

## Practical example

So far we've been working with the rather theoretical `Shape` example. Next, we'll take a look at something that you might actually encounter in the real world.

Let's assume we're building some kind of background processing service and we want to be able to query the current state of a background job along with relevant metadata. One way to model this state could be:

```cs
public enum JobState
{
    Pending,
    Running,
    Finished,
    Failed,
}

public record Job
{
    public Guid Id { get; init; }
    public JobState State { get; init; } // Current state of the job.
    public float Progress { get; init; } // Current progress. Percentage between 0 and 100.
    public byte[] Output { get; init; } // Result of the job.
    public string ErrorMessage { get; init; } // Reason why the job failed.
    public DateTime DeleteAfter { get; init; } // Automatically remove the job from the queue after this timestamp.
}
```

At first glance, this looks like perfectly fine, run-of-the-mill C# code. However, a few questions pop up:
- What is the value of `Output` when the job hasn't `Finished` yet? Is it null? Is it an empty array? Will it throw?
- Similarly for the `ErrorMessage` property: what will its value be when the job didn't fail?
- What is the `Progress` of a `Failed` job? `0`? `100`? The last progress before it failed? It throws? Who knows...

These issues could be resolved by simply adding more documentation and/or annotating the properties to be nullable. _Or,_ we can take advantage of the type system:

```cs
[EnumClass]
public abstract record JobState
{
    private JobState() {}

    public record Pending : JobState;
    public record Running(float Progress) : JobState;
    public record Finished(byte[] Output) : JobState;
    public record Failed(string ErrorMessage) : JobState;
}

public record Job
{
    public Guid Id { get; init; }
    public JobState State { get; init; }
    public DateTime DeleteAfter { get; init; }
}
```

In this new design, all properties that were dependent on the `State` have been pushed into the `JobState` type. This answers all of our earlier questions:
- only finished jobs have an `Output`,
- only failed jobs have an `ErrorMessage`,
- only in-progress jobs report their `Progress`.

Preconditions that previously only lived within comments or inside the heads of developers are now codified in the type system. And if you didn't notice already: **we've eliminated the need for any nullability or exceptions**. I.e. if a job has `Finished` it definitely has an `Output`, if a job has `Failed` it definitely has an `ErrorMessage`, etc.

## Comparison with interfaces

Both enum classes and interfaces can be used to represent _"one of multiple things"_. I've tried to summarize the distinction below:

<table>
    <thead>
        <tr>
            <th style="width: 50%;">
                Enum classes<br/>
                <sub>(and regular enums)</sub>
            </th>
            <th style="width: 50%;">
                Interfaces<br/>
                <sub>(and publicly extendable abstract base classes)</sub>
            </th>
        </tr>
    </thead>
    <tbody>
        <tr>
            <td>
                Typical usage: as a <b>concrete data</b> type.
            </td>
            <td>
                Typical usage: to <b>abstract</b> away <b>behavior</b>.
            </td>
        </tr>
        <tr>
            <td>
                The set of possible cases is <b>closed</b><br/> and known at <b>compile time</b>.
            </td>
            <td>
                The set of possible implementations is <b>open</b><br/> and can't be known until <b>run time</b>.
            </td>
        </tr>
        <tr>
            <td>
                The cases are part of the <b>public contract</b>.<br/> Consumers need to be aware of them.
            </td>
            <td>
                The implementations are an <b>implementation detail</b>.<br/> Consumers of the interface shouldn't need to be aware of them.
            </td>
        </tr>
        <tr>
            <td>
                Adding a <i>new operation</i> to an existing enum class:
                <ul>
                    <li>✅ is backwards-compatible</li>
                    <li>✅ can be done by anyone & anywhere</li>
                    <li>✅ implementation lives in only a single place</li>
                </ul>
            </td>
            <td>
                Adding a <i>new operation</i> to an existing interface:
                <ul>
                    <li>⛔ is backwards-incompatible</li>
                    <li>⛔ can only be done by the owner of the interface</li>
                    <li>⛔ implementation is scattered around in many places</li>
                </ul>
            </td>
        </tr>
        <tr>
            <td>
                Adding a <i>new case</i> to an existing enum class:
                <ul>
                    <li>⛔ is backwards-incompatible</li>
                    <li>⛔ can only be done by the owner of the enum class</li>
                    <li>⛔ all places consuming the type need to be aware of this change</li>
                </ul>
            </td>
            <td>
                Adding a <i>new implementation</i> for an existing interface:
                <ul>
                    <li>✅ is backwards-compatible</li>
                    <li>✅ can be done by anyone & anywhere</li>
                    <li>✅ implementation lives in only a single place</li>
                </ul>
            </td>
        </tr>
        <tr>
            <td colspan="2" style="text-align: center; font-style: italic;">
                As always, the real world isn't as black and white as this table makes it out to be.<br/>
                Interfaces can define methods with default implementations and<br/>
                enum classes can (ab)use inheritance between their base type & case types and/or even implement interfaces themselves.<br/>
                ¯\_(ツ)_/¯
            </td>
        </tr>
    </tbody>
</table>

## In other languages

Depending on which corner of the internet you come from, you might also know "enum classes" by different names:
- _"Sum types"_
- _"Tagged unions"_
- _"Discriminated unions"_
- _"Closed type hierarchies"_
- _"Sealed classes"_ <sub>(completely unrelated to C#'s concept of 'sealed' classes...)</sub>
- _"Algebraic Data Types"_
- _"Variants"_
- Or even simply: _"enums"_

Languages with built-in support:

- [Rust](https://doc.rust-lang.org/rust-by-example/custom_types/enum.html)
- [Swift](https://docs.swift.org/swift-book/documentation/the-swift-programming-language/enumerations/#Associated-Values)
- [Kotlin](https://kotlinlang.org/docs/sealed-classes.html)
- [Java](https://www.baeldung.com/java-sealed-classes-interfaces)
- [Scala](https://docs.scala-lang.org/scala3/reference/enums/adts.html)
- [TypeScript](https://www.typescriptlang.org/docs/handbook/unions-and-intersections.html#discriminating-unions)
- _(many functional languages...)_