[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
[<RequireQualifiedAccess>]
module FSharpWrap.Tool.Reflection.TypeArg

let fsname tname =
    function
    | TypeParam { Name = name } -> sprintf "'``%s``" name
    | TypeArg tref -> tname tref
