namespace FSharpWrap.Tool

open System
open System.Reflection

[<AutoOpen>]
[<Obsolete>]
module Collections =
    let inline (|Empty|NotEmpty|) col =
        if (^T : (member IsEmpty : bool) col)
        then Choice1Of2()
        else Choice2Of2 col

    let inline (|ContainsValue|_|) key col =
        let mutable value = Unchecked.defaultof<'Value>
        if (^T : (member TryGetValue : 'Key * byref<'Value> -> bool) (col, key, &value))
        then Some value
        else None

[<RequireQualifiedAccess>]
module String =
    let toCamelCase =
        String.mapi
            (function
            | 0 -> Char.ToLower
            | _ -> id)

    let toLiteral (str: string) =
        str.Replace("\"", "\"\"") |> sprintf " @\"%s\""

[<RequireQualifiedAccess>]
module Attribute =
    let find ns name chooser (source: seq<CustomAttributeData>) =
        source
        |> Seq.where
            (fun attr ->
                let t = attr.AttributeType
                t.Name = name && t.Namespace = ns)
        |> Seq.tryPick chooser

    let ctorArgs<'arg> (data: CustomAttributeData) =
        data.ConstructorArguments
        |> Seq.map
            (fun arg ->
                match arg.Value with
                | :? 'arg as arg' -> Some arg'
                | _ -> None)
        |> List.ofSeq

[<RequireQualifiedAccess>]
module MemberInfo =
    let findAttr ns name chooser (m: MemberInfo) =
         m.GetCustomAttributesData() |> Attribute.find ns name chooser

    let compiledName (mber: MemberInfo) =
        findAttr
            "Microsoft.FSharp.Core"
            "CompiledNameAttribute"
            (fun attr ->
                attr.ConstructorArguments
                |> Seq.map (fun arg -> arg.Value)
                |> Seq.tryHead
                |> Option.bind
                    (function
                    | :? string as str ->
                        if str.StartsWith "FSharp" then
                            str.Substring 6
                        else
                            // TODO: Get actual name of type from the F# metadata.
                            // sprintf "Cannot original name of member %s" mber.Name |> invalidOp
                            str
                        |> Some
                    | _ -> None))
            mber
        |> Option.defaultValue mber.Name
