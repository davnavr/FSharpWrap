module Program

open System.Collections.Generic
open System.Diagnostics

open CSharpDependency

let notExcluded() =
    StackTrace() |> StackTrace.getFrame 0

let collection() =
    MyCustomList.expr {
        1
        yield! [ 2..5 ]
        if Unchecked.defaultof<obj> = null then
            6
    }

[<EntryPoint>]
let main _ =
    let msg = MyString.ofString "Hello"
    msg
    |> MyString.value
    |> printfn "%s"
    
    let alphabet =
        List.expr {
            'a'
            'b'
            'c'
            yield! [ 'd'..'z' ]
        }

    Seq.iter (printfn "%c") alphabet
    0 // return an integer exit code
