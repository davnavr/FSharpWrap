[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
[<RequireQualifiedAccess>]
module FSharpWrap.Tool.Reflection.TypeRef

let fsname (t: TypeRef) =
    // TODO: Factor out common code for surrounding a full name with `` ``.
    sprintf
        "global."

let ofType (t: System.Type) =
    { Name =
        if t.IsGenericTypeDefinition then
            t.Name.LastIndexOf '`' |> t.Name.Remove
        else
            t.Name
      Namespace = Namespace.ofStr t.Namespace
      TypeArgs = invalidOp "type args" }
