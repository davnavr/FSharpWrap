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
    { Name =
        match t.DeclaringType with
        | null when t.IsGenericType ->
            t.Name.LastIndexOf '`' |> t.Name.Remove
        | _ ->
            t.Name
      Namespace = Namespace.ofStr t.Namespace
      Parent =
        t.DeclaringType
        |> Option.ofObj
        |> Option.map ofType
      TypeArgs =
        t.GetGenericArguments() // TODO: Fix, nested types inside generic types will get the generic arguments of the parent.
        |> List.ofArray
        |> List.map
            (function
            | GenericParam as tparam ->
                TypeParam.ofType tparam |> TypeParam
            | GenericArg as targ ->
                ofType targ |> TypeArg) }
