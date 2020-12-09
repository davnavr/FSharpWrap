(*** hide ***)
(*
index=1
*)
#r "../../packages/documentation/System.Collections.Immutable/lib/netstandard2.0/System.Collections.Immutable.dll"
#load "../content/output.autogen.fs"
(**
# Getting started

## Installing the package

To install the latest version of FSharpWrap using the
[.NET CLI](https://docs.microsoft.com/en-us/dotnet/core/tools/dotnet-add-package),
use the following:

```bash
dotnet add package FSharpWrap
```

## Using generated code

In order to use the generated code in your project, add the following element
to your project file in an `<ItemGroup>` above all other `<Compile>` items:

```xml
<Compile Include="$(FSharpWrapOutputFile)" />
```

Because the generated code is quite large by default, the following should also
be added to a `.gitignore` file:

```text
*.autogen.fs
```

Any instance methods, constructors, instance read-only fields, and get-only
properties on classes and immutable structs will then have F# functions, active
patterns, or [computation expressions](./collection-computation-expressions) generated for them.
*)
open System.Collections.Generic
open System.Collections.Immutable

// Constructors
let stack: Stack<_> = [ 1..10 ] |> Stack.ofSeq

// Instance method call
let a: bool = Stack.contains 5 stack

// Get-only properties
let b: int = Stack.count stack
let c: string =
    let nums = stack.ToImmutableList()
    match nums with
    | ImmutableList.IsEmpty -> "read-only"
    | _ -> "not read-only"

// Computation expressions
let d: Stack<_> =
    Stack.expr {
        for num in stack do num
        11;
        12;
        13;
    }

d :> seq<_> |> printfn "d is %A"
(*** include-output ***)
(**
## Filtering generated code

FSharpWrap allows entire assemblies and namespaces to be included or excluded
from code generation, which helps reduce compilation times by generating only
code that you will use. Note that assemblies can only either be included or
excluded, and that namespaces can only either be included or excluded.

To only include certain assemblies in code generation, use the following:
```xml
<ItemGroup>
  <!-- Only assemblies with the following names will be included in code generation -->
  <FSharpWrapIncludeNames Include="System.Collections.Immutable;Expecto" />
</ItemGroup>
```

To only exclude certain assemblies from code generation, use the following:

```xml
<ItemGroup>
  <FSharpWrapExcludeNames Include="System.Reflection.MetadataLoadContext;FSharp.Core" />
</ItemGroup>
```

Following the inclusion or exclusion of assemblies, FSharpWrap will also
include or exclude any specified namespaces. Note that in order to include or
exclude any "child" namespaces, their names must be specified as well.

To only include the types in certain namespaces, use the following:
```xml
<ItemGroup>
  <FSharpWrapIncludeNamespaces Include="System.Collections.Immutable;System.Collections" />
</ItemGroup>
```

To only exclude types in certain namespaces, use the following:
```xml
<ItemGroup>
  <!-- Only the types in the System.Reflection, Microsoft.FSharp.Core, or FParsec namespaces are excluded -->
  <FSharpWrapExcludeNamespaces Include="System.Reflection;Microsoft.FSharp.Core;FParsec" />
</ItemGroup>
```

## Code Generation Limitations
Currently, any non-readonly structs (structs not marked
[`IsReadOnly`](https://docs.microsoft.com/en-us/dotnet/fsharp/language-reference/structures#readonly-structs) in F#)
are excluded from code generation.
*)
