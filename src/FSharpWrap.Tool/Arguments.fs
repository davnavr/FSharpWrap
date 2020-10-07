namespace FSharpWrap.Tool

type Arguments =
    { AssemblyPaths: string list
      ProjectPath: string option }

[<RequireQualifiedAccess>]
module Arguments =
    type private State =
        | AssemblyPaths

    let rec private parseloop args state =
        match args with
        | [] -> fun _ -> state
        | h :: tail ->
            function
            | AssemblyPaths ->
                parseloop tail { state with ProjectPath = Some h } AssemblyPaths

    let parse argv =
        parseloop
            (List.ofArray argv)
            { AssemblyPaths = []
              ProjectPath = None }
            AssemblyPaths
