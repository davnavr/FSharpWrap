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
        | OutputFile
        | Invalid of string option
        | Unknown

    type private State =
        { AssemblyPaths: string list
          ExcludeNamespaces: string list
          OutputFile: string
          Type: StateType }

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
          ArgValue: string
          Description: string
          State: StateType }

    let all =
        [
            Optional "help", Invalid None, "Shows this help message", ""
            // TODO: Make this option accept directories as well.
            Required "assembly-paths", AssemblyPaths, "Specifies the paths to the assemblies", "path list"
            // TODO: Add argument to exclude some assemblies.
            // Optional "excluded-assemblies", ExcludedAssemblies, "Specifies the names of the assemblies to exclude from module generation", "name list"
            Required "output-file", OutputFile, "Specifies the path to the file containing the generated F# code", "file"
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
        let rec inner state args =
            match (state.Type, args) with
            | (Invalid msg, _) -> Error msg
            | (_, []) ->
                match state with
                | { AssemblyPaths = (Path.Valid phead) :: (Path.ValidList ptail)
                    OutputFile = Path.Valid out } ->
                    { AssemblyPaths = phead, ptail
                      ExcludeNamespaces = state.ExcludeNamespaces
                      OutputFile = out }
                    |> Ok
                | { OutputFile = Path.Invalid } ->
                    Some "The path to the output file is invalid" |> Error
                | { AssemblyPaths = _ } ->
                    Some "One or more paths to the assemblies are invalid" |> Error
            | (_, arg :: tail) ->
                let state' =
                    match (arg, state.Type) with
                    | (Argument arg', _) -> { state with Type = arg'.State }
                    | (path, AssemblyPaths) ->
                        { state with AssemblyPaths = path :: state.AssemblyPaths }
                    | (path, OutputFile) ->
                        { state with OutputFile = path; Type = Unknown }
                    | _ ->
                        { state with
                            Type =
                                sprintf "Unknown option '%s'" arg
                                |> Some
                                |> Invalid }
                inner state' tail
        inner
            { AssemblyPaths = []
              ExcludeNamespaces = []
              OutputFile = ""
              Type = Unknown }
