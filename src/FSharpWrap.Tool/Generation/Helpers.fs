[<AutoOpen>]
[<System.Obsolete>]
module private FSharpWrap.Tool.Generation.Helpers

open FSharpWrap.Tool.Reflection

let (|ReadOnlyField|_|) =
    function
    | { Field.IsReadOnly = ReadOnly } as field -> Some field
    | _ -> None

let indented lines = Seq.map (sprintf "    %s") lines

let block lines =
    seq {
        yield "begin"
        yield! indented lines
        yield "end"
    }

let attr name args =
    sprintf "[<%s(%s)>]" name args
