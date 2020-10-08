namespace FSharpWrap.Tool

open System
open System.ComponentModel
open System.IO

[<RequireQualifiedAccess>]
module Path =
    [<EditorBrowsable(EditorBrowsableState.Never)>]
    [<StructuralComparison; StructuralEquality>]
    type Path =
        private
        | Path of string

        override this.ToString() = let (Path path) = this in path

    let ofStr =
        function
        | "" -> None
        | str ->
            try
                Path.GetFullPath str |> Path |> Some
            with
            | :? ArgumentException
            | :? NotSupportedException
            | :? PathTooLongException -> None

    let (|Valid|Invalid|) =
        ofStr
        >> Option.map Choice1Of2
        >> Option.defaultValue (Choice2Of2())
    let (|ValidList|_|) =
        let rec inner state =
            function
            | [] -> Some state
            | (Valid path) :: tail ->
                let state' = path :: state
                inner state' tail
            | _ -> None
        inner []

type Path = Path.Path
