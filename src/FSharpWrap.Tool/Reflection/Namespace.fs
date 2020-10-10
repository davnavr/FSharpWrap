namespace FSharpWrap.Tool.Reflection

[<StructuralComparison; StructuralEquality>]
type Namespace =
    | Namespace of string list

    override this.ToString() =
        let (Namespace strs) = this in String.concat "." strs

[<RequireQualifiedAccess>]
module Namespace =
    let ofStr =
        function
        | "" -> []
        | str ->
            str.Split '.'
            |> List.ofArray
        >> Namespace

    let identifier (Namespace strs) =
        match strs with
        | [] -> "global"
        | _ ->
            List.map
                (sprintf "``%s``")
                strs
            |> String.concat "."
