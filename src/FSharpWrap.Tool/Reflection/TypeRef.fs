[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
[<RequireQualifiedAccess>]
module FSharpWrap.Tool.Reflection.TypeRef

let fsname (t: TypeRef) =
    match t.TypeArgs with
    | [] -> ""
    | targs ->
        List.map (fun _ -> "_") targs
        |> String.concat ", "
        |> sprintf "<%s>"
    |> sprintf
        "%s.%s%s"
        (Namespace.identifier t.Namespace)
        t.Name

let rec ofType (t: System.Type) =
    { Name = t.Name // TODO: How to handle names of generic types or nested types inside of generic types?
      Namespace = Namespace.ofStr t.Namespace
      TypeArgs =
        t.GetGenericArguments()
        |> List.ofArray
        |> List.map
            (function
            | GenericParam as tparam ->
                TypeParam.ofType tparam |> TypeParam
            | GenericArg as targ ->
                ofType targ |> TypeArg) }
