namespace FSharpWrap.Tool.Reflection

[<StructuralComparison; StructuralEquality>]
type Namespace = Namespace of FsName list

[<RequireQualifiedAccess>]
module Namespace =
    let print (Namespace strs) =
        match strs with
        | [] -> "global"
        | _ ->
            List.map
                FsName.print
                strs
            |> String.concat "."

    let ofStr =
        function
        | "" -> []
        | str ->
            str.Split '.'
            |> List.ofArray
            |> List.choose FsName.ofStr
        >> Namespace
