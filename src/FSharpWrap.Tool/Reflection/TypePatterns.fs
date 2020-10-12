[<AutoOpen>]
module private FSharpWrap.Tool.Reflection.TypePatterns

open System

let (|GenericParam|GenericArg|) (t: Type) =
    if t.IsGenericParameter then Choice1Of2() else Choice2Of2()

let (|GenericArgs|) (t: Type) = t.GetGenericArguments()
