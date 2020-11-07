module Program

open CSharpDependency

[<EntryPoint>]
let main _ =
    let msg = MyString.ofString "Hello"
    msg
    |> MyString.value
    |> printfn "%s"
    0 // return an integer exit code
