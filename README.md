# FSharpWrap
[![Build Status](https://github.com/davnavr/FSharpWrap/workflows/Build/badge.svg)](https://github.com/davnavr/FSharpWrap/actions?query=workflow%3ABuild)
[![Nuget](https://img.shields.io/nuget/v/FSharpWrap)](https://www.nuget.org/packages/FSharpWrap/)
![GitHub top language](https://img.shields.io/github/languages/top/davnavr/fsharpwrap)
[![Project Status: WIP – Initial development is in progress, but there has not yet been a stable, usable release suitable for the public.](https://www.repostatus.org/badges/latest/wip.svg)](https://www.repostatus.org/#wip)

Utility that automatically generates F# modules, functions, and active patterns
based on your F# project file's references.

## Usage
For information on how to use and configure FSharpWrap,
[see the docs](https://davnavr.github.io/FSharpWrap/getting-started.html).

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
