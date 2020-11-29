module Program

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
    0 // return an integer exit code
