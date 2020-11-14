module Program

open System.Diagnostics

open CSharpDependency

let notExcluded() =
    StackTrace() |> StackTrace.getFrame 0

[<EntryPoint>]
let main _ =
    let msg = MyString.ofString "Hello"
    msg
    |> MyString.value
    |> printfn "%s"
    0 // return an integer exit code
