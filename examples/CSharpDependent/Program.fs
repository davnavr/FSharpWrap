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
            yield! [ 'a'..'z' ]
            for c in [ 'A'..'W' ] do
                yield c
            let mutable cchar = 'X'
            while (printfn "C %c" cchar; cchar <= 'Z') do // TODO: Fix infinite loop.
                printfn "A %c" cchar
                yield cchar
                cchar <- cchar + char 1
                printfn "B"
        }

    Seq.iter (printfn "%c") alphabet
    0 // return an integer exit code
