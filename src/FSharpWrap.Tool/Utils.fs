namespace FSharpWrap.Tool

open System.Reflection

[<AutoOpen>]
module internal Collections =
    let inline (|Empty|_|) col =
        if (^T : (member IsEmpty : bool) col)
        then Some()
        else None

    let inline (|ContainsValue|_|) key col =
        let mutable value = Unchecked.defaultof<'Value>
        if (^T : (member TryGetValue : 'Key * byref<'Value> -> bool) (col, key, &value))
        then Some value
        else None

[<RequireQualifiedAccess>]
module internal TypeInfo =
    let inline equal ns name (t: System.Type) =
        t.Name = name && t.Namespace = ns

[<RequireQualifiedAccess>]
module internal Attribute =
    let find ns name chooser (source: seq<CustomAttributeData>) =
        source
        |> Seq.where
            (fun attr -> TypeInfo.equal ns name attr.AttributeType)
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
module internal MemberInfo =
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
