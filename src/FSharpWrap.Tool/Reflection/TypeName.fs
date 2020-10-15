[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
[<RequireQualifiedAccess>]
module FSharpWrap.Tool.Reflection.TypeName

let fsname (t: TypeName) = // TODO: Include parent type if type is nested.
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

let rec ofType targs (GenericArgs gargs as t) =
    let parent = Option.ofObj t.DeclaringType
    { Name = SimpleName.ofType t
      Namespace = Namespace.ofStr t.Namespace
      Parent = Option.map (ofType targs) parent
      TypeArgs = targs parent gargs }
