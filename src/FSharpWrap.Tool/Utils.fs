namespace FSharpWrap.Tool

[<AutoOpen>]
module Collections =
    let inline (|ContainsValue|_|) key col =
        let mutable value = Unchecked.defaultof<'Value>
        if (^T : (member TryGetValue : 'Key * byref<'Value> -> bool) (col, key, &value))
        then Some value
        else None
