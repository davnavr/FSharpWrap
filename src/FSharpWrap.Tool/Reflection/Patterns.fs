[<AutoOpen>]
module private FSharpWrap.Tool.Reflection.TypePatterns

open System
open System.Reflection

let (|GenericParam|GenericArg|) (t: Type) =
    if t.IsGenericParameter then Choice1Of2() else Choice2Of2()

let (|GenericArgs|) (t: Type) = t.GetGenericArguments()

let (|IsArray|_|) (t: Type) =
    if t.IsArray then t.GetElementType() |> Some else None

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
