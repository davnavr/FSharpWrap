[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
[<RequireQualifiedAccess>]
module FSharpWrap.Tool.Reflection.TypeRef

let fsname (t: TypeRef) = // TODO: Include parent type if type is nested.
    match t.TypeArgs with
    | [] -> ""
    | targs ->
        List.map (fun _ -> "_") targs
        |> String.concat ", "
        |> sprintf "<%s>"
    |> sprintf
        "%s.%s%s"
        (Namespace.identifier t.Namespace)
        (SimpleName.fsname t.Name)

let rec ofType (GenericArgs gargs as t) =
    let parent = Option.ofObj t.DeclaringType
    { Name = SimpleName.ofType t
      Namespace = Namespace.ofStr t.Namespace
      Parent = Option.map ofType parent
      TypeArgs =
        let inherited =
            parent
            |> Option.map (|GenericArgs|)
            |> Option.defaultValue Array.empty
        gargs
        |> List.ofArray
        |> List.except inherited
        |> List.map
            (function
            | GenericParam as tparam ->
                TypeParam.ofType tparam |> TypeParam
            | GenericArg as targ ->
                ofType targ |> TypeArg) }
