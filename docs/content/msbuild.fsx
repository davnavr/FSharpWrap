(*** hide ***)
(*
index=3
*)
(**
# MSBuild properties and items

FSharpWrap offers several properties and items to configure how it generates
code.

## Items
### `<FSharpWrapExcludeNames>` `<FSharpWrapIncludeNames>`
Specifies the names of the assemblies to include or exclude from code
generation. By default, all assemblies are included in code generation.
Using both items is an error.

### `<FSharpWrapExcludeNamespaces>` `<FSharpWrapIncludeNamespaces>`
Specifies the namespaces to included or exclude from code generation. By default,
all namespaces are included in code generation. Using both items is an error.

## Properties
### `<FSharpWrapOutputFile>`
The path to the output file containing the generated F# code. Defaults to
`$(MSBuildProjectDirectory)/output.autogen.fs` for projects with a single
`<TargetFramework>` or `$(MSBuildProjectDirectory)/output.$(TargetFramework).autogen.fs`
for projects using `<TargetFrameworks>`.

### `<_FSharpWrapLaunchDebugger>`
Property for internal use. If set to `true`, calls
[Debugger.Launch](https://docs.microsoft.com/en-us/dotnet/api/system.diagnostics.debugger.launch).
*)
