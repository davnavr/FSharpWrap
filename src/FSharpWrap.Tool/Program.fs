[<RequireQualifiedAccess>]
module FSharpWrap.Tool.Program

let private help =
    [
        "Usage: fsharpwrap [--help]"
        ""
        "Options:"
        for arg in Arguments.all do
            sprintf "    --%s %s" arg.Key arg.Value.Description // TODO: Align descriptions.
    ]

[<EntryPoint>]
let main argv =
    match List.ofArray argv |> Arguments.parse with
    | Some args -> 0
    | None ->
        List.iter (printfn "%s") help
        -1
