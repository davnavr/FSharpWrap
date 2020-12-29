[<AutoOpen>]
module FSharpWrap.Tool.MemberPatterns

open System
open System.Reflection

let (|Constructor|Event|Field|Method|Property|Type|) (mber: MemberInfo) =
    match mber with
    | :? ConstructorInfo as c -> Choice1Of6 c
    | :? EventInfo as e -> Choice2Of6 e
    | :? FieldInfo as f -> Choice3Of6 f
    | :? MethodInfo as m -> Choice4Of6 m
    | :? PropertyInfo as p -> Choice5Of6 p
    | :? Type as t -> Choice6Of6 t
    | _ ->
        mber.GetType()
        |> sprintf "Unknown member type %O"
        |> invalidArg "mber"

let inline (|Static|Instance|) (mber: ^T) =
    if (^T : (member IsStatic: bool) mber) then Choice1Of2 mber else Choice2Of2 mber

let (|Indexer|_|) (prop: PropertyInfo) =
    if prop.GetIndexParameters() |> Array.isEmpty
    then Some prop
    else None
