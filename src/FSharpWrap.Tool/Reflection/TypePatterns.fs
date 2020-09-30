[<AutoOpen>]
module FSharpWrap.Tool.Reflection.TypePatterns

open System

let (|GlobalType|NestedType|) (tdef: Type) =
    tdef |> if tdef.IsNested then Choice1Of2 else Choice2Of2
