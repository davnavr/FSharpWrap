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

    type ArgumentType =
        | Optional of string
        | Required of string

        member this.Name =
            match this with
            | Optional name
            | Required name -> name

        override this.ToString() =
            match this with
            | Optional name -> sprintf "[--%s]" name
            | Required name -> sprintf "--%s" name

    type Info =
        { ArgType: ArgumentType
          ArgValue: string option
          Description: string
          State: StateType }

    let all =
        [
            Optional "help", Invalid, "Shows this help message", None
            Required "assembly-paths", AssemblyPaths, "Specifies the paths to the assemblies to generate F# code for", Some "paths"
            Required "output-file", OutputFile, "Specifies the path to the file containing the generated F# code", Some "file"
        ]
        |> Seq.map (fun (name, st, desc, value) ->
            let info =
                { ArgType = name
                  ArgValue = value
                  Description = desc
                  State = st }
            name.Name, info)
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
                    | (Argument arg', _) -> { state with Type = arg'.State }
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
