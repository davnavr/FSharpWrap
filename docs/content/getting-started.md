1
# Getting Started

## Installing the package
Insert some content here.

## Using generated code

In order to use the generated code in your project, add the following element
to your project file in an `<ItemGroup>`:

```xml
<Compile Include="$(FSharpWrapOutputFile)" />
```

Because the generated code is quite large by default, the following should also
be added to your `.gitignore` file:

```text
*.autogen.fs
```

Insert explanation here.

```
module MyExampleModule

open System.Collections.Generic

let a =
    [ 1..10 ] |> Stack.ofSeq
```

Add more explanations and examples.

## Filtering Generated Code

FSharpWrap allows entire assemblies and namespaces to be included or excluded
from code generation, which helps reduce compilation times by generating only
code that you will use. Note that assemblies can only either be included or
excluded, and that namespaces can only either be included or excluded.

To only include certain assemblies in code generation, use the following:
```xml
<ItemGroup>
  <!--  -->
  <FSharpWrapIncludeNames Include="System.Collections.Immutable" />
</ItemGroup>
```

To only exclude certain assemblies from code generation, use the following:
```xml
<ItemGroup>
  <!-- Only types in the System, Microsoft.FSharp.Core, or FParsec namespaces are excluded -->
  <FSharpWrapExcludeNames Include="System;Microsoft.FSharp.Core;FParsec" />
</ItemGroup>
```

Following the inclusion or exclusion of assemblies, FSharpWrap will also
include or exclude any specified namespaces.

To only include the types in certain namespaces, use the following:
```xml
<ItemGroup>
  <!--  -->
  <FSharpWrapIncludeNamespaces Include="System.Collections.Immutable" />
</ItemGroup>
```
