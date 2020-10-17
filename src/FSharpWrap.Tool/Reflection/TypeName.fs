[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
[<RequireQualifiedAccess>]
module FSharpWrap.Tool.Reflection.TypeName

let rec print t =
    let name =
        match TypeArgList.toList t.TypeArgs with
        | [] -> ""
        | targs ->
            List.map
                (function
                | TypeParam -> "_"
                | TypeArg targ -> invalidOp "bad") // TODO: How to print TypeRef when the function is unavailable?
                targs
            |> String.concat ","
            |> sprintf "<%s>"
        |> sprintf
            "%s%s"
            (FsName.print t.Name)
    match t with
    | { Parent = None } ->
        id
    | { Parent = Some parent } ->
        sprintf "%s.%s" (print parent)
    <| name

let rec ofType targs (GenericArgs gargs as t) =
    let parent = Option.ofObj t.DeclaringType
    { Name = FsName.ofType t
      Namespace = Namespace.ofStr t.Namespace
      Parent = Option.map (ofType targs) parent
      TypeArgs = targs parent gargs }
