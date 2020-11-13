namespace FSharpWrap.Tool

[<StructuralComparison; StructuralEquality>]
type Namespace =
    | Namespace of FsName list

    override this.ToString() =
        let (Namespace ns) = this
        ns
        |> List.map (sprintf "%O")
        |> String.concat "."

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
