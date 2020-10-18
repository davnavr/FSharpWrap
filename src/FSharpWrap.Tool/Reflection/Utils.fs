namespace FSharpWrap.Tool.Reflection

open System
open System.Reflection

[<AutoOpen>]
module private Patterns =
    let (|GenericParam|GenericArg|) (t: Type) =
        if t.IsGenericParameter then Choice1Of2() else Choice2Of2()

    let (|GenericArgs|) (t: Type) = t.GetGenericArguments()

    let (|IsArray|_|) (t: Type) =
        if t.IsArray then t.GetElementType() |> Some else None

    let (|IsByRef|_|) (t: Type) =
        if t.IsByRef then t.GetElementType() |> Some else None

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

[<RequireQualifiedAccess>]
module internal MemberInfo =
    let findAttr ns name chooser (m: MemberInfo) =
       m.GetCustomAttributesData()
       |> Seq.where
           (fun attr ->
               let t = attr.AttributeType
               t.Name = name && t.Namespace = ns)
       |> Seq.tryPick chooser

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
