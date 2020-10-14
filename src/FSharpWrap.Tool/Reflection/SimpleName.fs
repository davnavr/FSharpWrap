namespace FSharpWrap.Tool.Reflection

open System.Reflection

[<Struct; StructuralComparison; StructuralEquality>]
type SimpleName =
    | SimpleName of string

    override this.ToString() =
        let (SimpleName name) = this in name

[<RequireQualifiedAccess>]
module SimpleName =
    let fsname (SimpleName str) = sprintf "``%s``" str

    let ofStr =
        function
        | null | "" -> None
        | str -> SimpleName str |> Some

    let ofType (t: System.Type) =
        match t.DeclaringType, t.Name with
        | (_, "")
        | (_, null) -> invalidArg "t" "The name of the type was null or empty"
        | (null, name) when t.IsGenericType ->
            name.LastIndexOf '`' |> name.Remove
        | _ -> t.Name
        |> SimpleName

    let ofParameter (param: ParameterInfo) =
        ofStr param.Name |> Option.defaultValue (SimpleName "arg")
