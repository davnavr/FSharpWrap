namespace FSharpWrap.Tool

[<StructuralComparison; StructuralEquality>]
type Namespace = Namespace of FsName list

[<RequireQualifiedAccess>]
module Namespace =
    let ofStr =
        function
        | "" -> []
        | str ->
            str.Split '.'
            |> List.ofArray
            |> List.choose FsName.ofStr
        >> Namespace
