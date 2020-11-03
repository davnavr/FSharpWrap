﻿[<AutoOpen>]
module private FSharpWrap.Tool.Reflection.MemberPatterns

open System
open System.Reflection

open FSharpWrap.Tool

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

let (|IsStatic|_|) =
    function
    | (t: Type) when t.IsSealed && t.IsAbstract -> Some t
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

let (|IsFSharpModule|_|) (t: Type) =
    MemberInfo.findAttr
        "Microsoft.FSharp.Core"
        "CompilationMappingAttribute"
        (fun attr ->
            let arg = Seq.tryHead attr.ConstructorArguments
            match t, arg with
            | IsStatic _, Some arg' when arg'.Value = (int SourceConstructFlags.Module |> box) ->
                Some t
            | _ -> None)
        t
