[<RequireQualifiedAccess>]
module FSharpWrap.Tool.Reflection.Type

open System

let simpleName (t: Type) =
    if t.IsGenericTypeDefinition then
        t.Name.LastIndexOf(''') |> t.Name.Remove
    else
        t.Name
