namespace FSharpWrap.Tool.Reflection

open System
open System.Reflection

[<RequireQualifiedAccess>]
module internal TypeInfo =
    let inline equal ns name (t: Type) =
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

[<AutoOpen>]
module private MemberPatterns =
    let (|GenericParam|GenericArg|) (t: Type) =
        if t.IsGenericParameter
        then t.GetGenericParameterConstraints() |> Choice1Of2
        else Choice2Of2()

    let (|GenericArgs|) (t: Type) = t.GetGenericArguments()

    let (|IsArray|_|) (t: Type) =
        if t.IsArray then t.GetElementType() |> Some else None

    let (|IsByRef|_|) (t: Type) =
        if t.IsByRef then t.GetElementType() |> Some else None

    let (|IsCompilerGenerated|_|): MemberInfo -> _ =
        MemberInfo.findAttr
            "System.Runtime.CompilerServices"
            "CompilerGeneratedAttribute"
            (fun _ -> Some ())

    let (|DebuggerBrowsable|NeverDebuggerBrowsable|): MemberInfo -> _ =
        MemberInfo.findAttr
            "System.Diagnostics"
            "DebuggerBrowsableAttribute"
            (fun data ->
                match Attribute.ctorArgs data with
                | [ Some 0 ] -> Choice2Of2() |> Some // Never
                | _ -> None)
        >> Option.defaultWith Choice1Of2

    let (|IsNested|_|) (t: Type) = if t.IsNested then Some() else None

    let (|IsPointer|_|) (t: Type) =
        if t.IsPointer then t.GetElementType() |> Some else None

    let (|IsSpecialName|_|): MemberInfo -> _ =
        function
        | :? MethodBase as mthd when mthd.IsSpecialName -> Some()
        | :? FieldInfo as field when field.IsSpecialName -> Some()
        | _ -> None

    let (|PropAccessor|_|): MemberInfo -> _ =
        let check (mthd: MethodInfo) =
            mthd.DeclaringType.GetProperties()
            |> Seq.collect (fun prop -> prop.GetAccessors())
            |> Seq.contains mthd
        function
        | :? MethodInfo as mthd when check mthd -> Some()
        | _ -> None

    let rec (|Derives|_|) ns name (t: Type) =
        let isBase = TypeInfo.equal ns name
        let tbase =
            t.BaseType
            |> Option.ofObj
            |> Option.bind
                (function
                | super when isBase super ->
                    Some super
                | Derives ns name indirect -> Some indirect
                | _ -> None)
        let intf = t.GetInterfaces() |> Array.tryFind isBase
        match tbase with
        | None -> intf
        | _ -> tbase

    let (|AssignableTo|_|) ns name =
        function
        | Derives ns name derived -> Some derived
        | t when TypeInfo.equal ns name t -> Some t
        | _ -> None

    let (|IsTuple|_|) =
        let (|IsTupleType|_|) (t: Type) =
            if t.Namespace = "System" && (t.Name.StartsWith "Tuple" || t.Name.StartsWith "ValueTuple")
            then Some t
            else None
        function
        | Derives "System.Runtime.CompilerServices" "ITuple" tuple
        | IsTupleType tuple -> Some tuple
        | _ -> None
