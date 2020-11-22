[<RequireQualifiedAccess>]
module FSharpWrap.Tool.Tests.Gen

open FsCheck

open System

open FSharpWrap.Tool

let ws =
    gen {
        let! len = Gen.choose(0, 10)
        return String.replicate len " "
    }

let chars: seq<char> -> _ =
    Gen.elements
    >> Gen.arrayOf
    >> Gen.resize 7
    >> Gen.map String

let path =
    let name =
        [
            [ 'a'..'z' ]
            [ 'A'..'Z' ]
            [ '0'..'9' ]
            [ '_'; ' ' ]
        ]
        |> List.collect id
        |> chars
    let sep = Gen.elements [ '\\'; '/' ]
    gen {
        let! drive =
            [
                [ 'A'..'E' ]
                |> Gen.elements
                |> Gen.map (sprintf "%c:")

                Gen.constant ""
            ]
            |> Gen.oneof
        let! dir =
            Gen.map2
                (sprintf "%s%c")
                name
                sep
            |> Gen.arrayOf
            |> Gen.resize 6
            |> Gen.map String.Concat
        let! file = name
        let! ext = Gen.resize 4 name
        return
            sprintf "%s%s%s.%s" drive dir file ext
            |> Path.ofStr
            |> Option.get
    }
