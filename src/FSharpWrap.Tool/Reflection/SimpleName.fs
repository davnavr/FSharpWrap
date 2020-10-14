namespace FSharpWrap.Tool.Reflection

[<Struct; StructuralComparison; StructuralEquality>]
type SimpleName =
    | SimpleName of string

    override this.ToString() =
        let (SimpleName name) = this in name

[<RequireQualifiedAccess>]
module SimpleName =
    let ofType (t: System.Type) =
        match t.DeclaringType with
        | null when t.IsGenericType ->
            t.Name.LastIndexOf '`' |> t.Name.Remove
        | _ -> t.Name
        |> SimpleName
