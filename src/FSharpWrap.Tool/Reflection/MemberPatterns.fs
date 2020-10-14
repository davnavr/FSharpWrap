[<AutoOpen>]
module internal FSharpWrap.Tool.Reflection.MemberPatterns

let (|ReadOnlyField|_|) =
    function
    | { Field.IsReadOnly = ReadOnly } as field -> Some field
    | _ -> None
