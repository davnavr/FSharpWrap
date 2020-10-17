namespace FSharpWrap.Tool.Reflection

open System.Reflection

[<Struct; StructuralComparison; StructuralEquality>]
type FsName =
    internal
    | FsName of string

    override this.ToString() =
        let (FsName name) = this in name

[<RequireQualifiedAccess>]
module FsName =
    let print (FsName name) = sprintf "``%s``" name

    let ofStr =
        function
        | null | "" -> None
        | str -> FsName str |> Some

    let ofType (t: System.Type) = // TODO: Return option instead of throwing exception?
        match t.DeclaringType, t.Name with
        | (_, "")
        | (_, null) -> invalidArg "t" "The name of the type was null or empty"
        | (null, name) when t.IsGenericType ->
            name.LastIndexOf '`' |> name.Remove
        | _ -> t.Name
        |> FsName

    let ofParameter (param: ParameterInfo) =
        ofStr param.Name |> Option.defaultValue (sprintf "_arg%i" param.Position |> FsName)
