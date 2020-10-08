[<RequireQualifiedAccess>]
module FSharpWrap.Tool.Program

let private help =
    [
        let args =
            Arguments.all
            |> Map.toSeq
            |> Seq.map snd
        let argstr f (arg: Arguments.Info) =
            match arg.ArgValue with
            | Some value' ->
                (fun s -> sprintf "%s <%s>" s value')
            | None -> id
            <| f arg.ArgType
        args
        |> Seq.map (argstr string)
        |> String.concat " "
        |> sprintf "Usage: fsharpwrap %s"
        ""
        "Options:"
        ""
        let (info, len) =
            args
            |> Seq.mapFold
                (fun len' arg ->
                    let name = argstr (fun arg' -> arg'.Name) arg
                    (name, arg.Description), max len' name.Length)
                0
        yield!
            info
            |> Seq.map (fun (name, desc) ->
                let ws =
                    String.replicate (len - name.Length) " "
                sprintf "    --%s %s%s" name ws desc)
            |> List.ofSeq
    ]

[<EntryPoint>]
let main argv =
    match List.ofArray argv |> Arguments.parse with
    | Some args -> 0
    | None ->
        List.iter (printfn "%s") help
        -1
