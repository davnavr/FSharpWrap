[<RequireQualifiedAccess>]
module FSharpWrap.Tool.Program

[<EntryPoint>]
let main argv =
    let args = Arguments.parse argv
    printfn "Hello World!"
    0
