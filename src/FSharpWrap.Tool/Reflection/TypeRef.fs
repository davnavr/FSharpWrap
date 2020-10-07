[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
[<RequireQualifiedAccess>]
module FSharpWrap.Tool.Reflection.TypeRef

let ofType (t: System.Type) =
    { Name =
        if t.IsGenericTypeDefinition then
            t.Name.LastIndexOf '`' |> t.Name.Remove
        else
            t.Name
      Namespace = t.Namespace
      TypeArgs = invalidOp "type args" }
