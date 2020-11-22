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

To exclude or include entire assemblies from code generation, use ONE of the following in your project file

```xml
<ItemGroup>
  <!-- This will exclude an assembly file whose file name is an exact match -->
  <FSharpWrapExcludeFiles Include="/Some/Path/To/An/Assembly.To.Exclude.dll" />
  <!-- This will exclude all .dll files in the directory and any subdirectories -->
  <FSharpWrapExcludeFiles Include="/Another/Path/To/Exclude/" />
</ItemGroup>
```
```xml
<ItemGroup>
  <!-- You can either include OR exclude assembly files, but NOT BOTH -->
  <FSharpWrapIncludeFiles Include="/A/Path/Or/Directory/With/Assemblies.dll"/>
</ItemGroup>
```

By default, only NuGet packages and project references are included in code generation.

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
