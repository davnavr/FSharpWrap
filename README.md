# FSharpWrap
[![Build Status](https://github.com/davnavr/FSharpWrap/workflows/Build/badge.svg)](https://github.com/davnavr/FSharpWrap/actions?query=workflow%3ABuild)
[![Nuget](https://img.shields.io/nuget/v/FSharpWrap)](https://www.nuget.org/packages/FSharpWrap/)
![GitHub top language](https://img.shields.io/github/languages/top/davnavr/fsharpwrap)
[![Project Status: WIP – Initial development is in progress, but there has not yet been a stable, usable release suitable for the public.](https://www.repostatus.org/badges/latest/wip.svg)](https://www.repostatus.org/#wip)

Utility that automatically generates F# modules and functions based on your F# project file's references.

## Usage
Add the following under an `<ItemGroup>` to your `.fsproj` project file

```xml
<PackageReference Include="FSharpWrap" Version="0.1.0">
  <PrivateAssets>all</PrivateAssets>
  <IncludeAssets>runtime; build; native; contentfiles; analyzers</IncludeAssets>
</PackageReference>

<!-- Make sure this comes before any of your *.fs files -->
<Compile Include="$(FSharpWrapOutputFileName)" />
```

Depending on your needs, you may also want to add the following to your `.gitignore` file

```text
*.autogen.fs
```

Assemblies can be either excluded or included from code generation, but not both

```xml
<ItemGroup>
  <!-- This will include only assemblies with the names A, B, or C in code generation -->
  <FSharpWrapIncludeNames Include="A;B;C" />
</ItemGroup>
```
```xml
<ItemGroup>
  <!-- This will exclude assemblies with the names D, E, or F, from code generation -->
  <FSharpWrapIncludeNames Include="D;E;F" />
</ItemGroup>
```

By default, all assemblies referenced by a project are included in code generation.

Namespaces can also be either included or excluded from code generation

```xml
<ItemGroup>
  <!--
    In the assemblies that will be included in code generation, only
    the types that are in the namespaces G, H, or I will be included
  -->
  <FSharpWrapIncludeNamespaces Include="G;H;I" />
</ItemGroup>
```
```xml
<ItemGroup>
  <!-- Types in the namespace J, K, or L are excluded from code generation -->
  <FSharpWrapExcludeNamespaces Include="J;K;L" />
</ItemGroup>
```

## Example
A dependency containing the following C# class:

```cs
public class ExampleDictionary<TKey, TValue>
{
    public void SetItem(TKey key, TValue value);
}
```

Could then be used in the following way:
```fs
let myDict = new ExampleDictionary<string, int>()
ExampleDictionary.setItem "hello" 5 myDict
```
