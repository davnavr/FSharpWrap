(*** hide ***)
(*
index=2
*)
(**
# Using multiple target frameworks

By default, FSharpWrap will generate different files for each target framework
used in your project.

## Code generation defaults

If only one target framework is specified using the `<TargetFramework>`
element, then the generated code is in a file named `output.autogen.fs` in
the current project directory. However, if multiple target frameworks are
specified using the
[`<TargetFrameworks>`](https://docs.microsoft.com/en-us/nuget/create-packages/multiple-target-frameworks-project-file)
element, then the path to each generated code file relative to the project
directory is `output.$(TargetFramework).autogen.fs`, where
`$(TargetFramework)` is the corresponding target framework moniker.

## Changing the file name

If you are only using one target framework, you can change the path to the
generated code file using the `<FSharpWrapOutputFile>` property.

```xml
<PropertyGroup>
  <FSharpWrapOutputFile>Some/Other/Directory/MyCustomFileName.autogen.fs</FSharpWrapOutputFile>
</PropertyGroup>
```

If using multiple target frameworks, consider inserting the
`$(TargetFramework)` in part of the path to the output file.

```xml
<PropertyGroup>
  <FSharpWrapOutputFile>CustomDirectory/MyCustomFileName.$(TargetFramework).autogen.fs</FSharpWrapOutputFile>
</PropertyGroup>
```
*)
