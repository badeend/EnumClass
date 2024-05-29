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
    Rectangle(float Width, float Length),
    Triangle(float SideLength),
}
```

Unfortunately, since we don't live in the future, this is the actual syntax we'll be working with today:
```cs
[EnumClass]
public abstract record Shape
{
    private Shape() {}

    public record Circle(float Radius) : Shape;
    public record Rectangle(float Width, float Length) : Shape;
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

All the magic happens at compile-time, as part of the analyzers shipped with this package.

Continuing with the example from above:
- We define a `Shape` "enum" type, that has three "case" types: `Circle`, `Rectangle` & `Triangle`.
- The `Shape` type has an `[EnumClass]` attribute, which is the cue for the analyzers to kick in.
- The analyzers enforce that the base type and nested subtypes satisfy all the required criteria for them to be worthy of the title "enum class". Some of these criteria can be seen right there in the example: `abstract` base type, private constructor, cases extend their parent type, etc... All for the ultimate goal:
- `Shape` is now **guarded against external extension** and we can be sure that any `Shape` instance we encounter at runtime will be: either a `Circle`, a `Rectangle` or a `Triangle`. Exactly one those three and _nothing more_.

Given that all the subtypes/cases are known at compile-time, we can enforce that any `switch`-expression/statement on them is **exhaustive**. I.e. we can warn developers when they've missed a case and provide them with a codefix to automatically add those missing cases. This may sound menial at first, but it has the potential to significantly improve the robustness of your application!

For more info, query your favorite search engine for: "Type-Driven Design" and "Make illegal states unrepresentable".


## The expression problem

Enum classes creep into the territory traditionally solely occupied by `interface`s. <sub>(or publicly extendable abstract base classes, but for all intents and purposes of this article I'll consider them to be equivalent to interfaces.)</sub>

Both enum classes and interfaces represent a type that can be "one of multiple things". The distinction is laid out below:

<table>
    <thead>
        <tr>
            <th style="width: 50%;">Enum classes</th>
            <th style="width: 50%;">Interfaces</th>
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
