# FSharpWrap
[![Build Status](https://github.com/davnavr/FSharpWrap/workflows/Build/badge.svg)](https://github.com/davnavr/FSharpWrap/actions?query=workflow%3ABuild)
![GitHub top language](https://img.shields.io/github/languages/top/davnavr/fsharpwrap)
[![Project Status: WIP – Initial development is in progress, but there has not yet been a stable, usable release suitable for the public.](https://www.repostatus.org/badges/latest/wip.svg)](https://www.repostatus.org/#wip)

Utility that automatically generates F# modules and functions based on your F# project file's references.

## Usage
Add the following to your `.fsproj` project file.

```xml
<PackageReference Include="FSharpWrap" Version="0.1.0">
  <PrivateAssets>all</PrivateAssets>
  <IncludeAssets>runtime; build; native; contentfiles; analyzers</IncludeAssets>
</PackageReference>
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
