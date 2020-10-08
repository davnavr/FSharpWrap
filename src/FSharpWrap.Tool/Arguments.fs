namespace FSharpWrap.Tool

type Arguments =
    { AssemblyPaths: Path * Path list
      ExcludeNamespaces: string list
      OutputFile: Path }

    member this.Assemblies =
        let h, tail = this.AssemblyPaths in h :: tail

[<RequireQualifiedAccess>]
module Arguments =
    type StateType =
        private
        | AssemblyPaths
        | Invalid
        | OutputFile
        | Unknown

    type private State =
        { AssemblyPaths: string list
          ExcludeNamespaces: string list
          OutputFile: string
          Type: StateType }

    [<RequireQualifiedAccess>]
    module private State = let invalidate st = { st with Type = Invalid }

    let all =
        [
            "help", Invalid, "Shows this help message"
        ]
        |> Seq.map (fun (name, t, desc) ->
            let info =
                {| Description = desc
                   Name = name
                   Type = t |}
            name, info)
        |> Map.ofSeq

    let parse =
        let (|Argument|_|) =
            function
            | (arg: string) when arg.StartsWith "--" ->
                let name =
                    arg
                        .Substring(2)
                        .TrimStart()
                        .TrimEnd()
                Map.tryFind name all
            | _ -> None
        let rec inner state =
            function
            | [] ->
                match state with
                | { Type = Invalid } -> None
                | { AssemblyPaths = (Path.Valid phead) :: (Path.ValidList ptail)
                    OutputFile = Path.Valid out } ->
                    { AssemblyPaths = phead, ptail
                      ExcludeNamespaces = state.ExcludeNamespaces
                      OutputFile = out }
                    |> Some
                | _ -> None
            | arg :: tail ->
                let state' =
                    match (arg, state.Type) with
                    | (Argument arg', _) -> { state with Type = arg'.Type }
                    | (path, AssemblyPaths) ->
                        { state with AssemblyPaths = path :: state.AssemblyPaths }
                    | (path, OutputFile) ->
                        { state with OutputFile = path; Type = Unknown }
                    | _ -> State.invalidate state
                inner state' tail
        inner
            { AssemblyPaths = []
              ExcludeNamespaces = []
              OutputFile = ""
              Type = Unknown }
