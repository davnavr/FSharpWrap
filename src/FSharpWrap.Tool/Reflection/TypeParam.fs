[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
[<RequireQualifiedAccess>]
module FSharpWrap.Tool.Reflection.TypeParam

let ofType (t: System.Type) =
    // TODO: Maybe check if the type is valid?
    { TypeParam.Name = t.Name }
