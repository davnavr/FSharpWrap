namespace FSharpWrap.Tool

[<AutoOpen>]
module Collections =
    let inline (|Empty|_|) col =
        if (^T : (member IsEmpty : bool) col)
        then Some()
        else None

    let inline (|ContainsValue|_|) key col =
        let mutable value = Unchecked.defaultof<'Value>
        if (^T : (member TryGetValue : 'Key * byref<'Value> -> bool) (col, key, &value))
        then Some value
        else None
