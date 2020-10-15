[<AutoOpen>]
module internal FSharpWrap.Tool.Reflection.MemberPatterns

open System.Reflection

let (|IsSpecialName|_|): MemberInfo -> _ =
    function
    | :? MethodBase as mthd when mthd.IsSpecialName -> Some()
    | _ -> None

let (|PropAccessor|_|): MemberInfo -> _ =
    let check (mthd: MethodInfo) =
        mthd.DeclaringType.GetProperties()
        |> Seq.collect (fun prop -> prop.GetAccessors())
        |> Seq.contains mthd
    function
    | :? MethodInfo as mthd when check mthd -> Some()
    | _ -> None

let (|ReadOnlyField|_|) =
    function
    | { Field.IsReadOnly = ReadOnly } as field -> Some field
    | _ -> None
