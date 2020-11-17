namespace FSharpWrap.Tool

type Excluded =
    { AssemblyFiles: Set<string>  }

type Arguments =
    { AssemblyPaths: Path * Path list
      Exclude: Excluded
      LaunchDebugger: bool
      OutputFile: Path }

    member this.Assemblies =
        let h, tail = this.AssemblyPaths in h :: tail

type InvalidArgument =
    | EmptyAssemblyPaths
    | InvalidAssemblyPath of string
    | InvalidOutputFile of string
    | NoOutputFile
    | ShowUsage
    | UnknownOption of string

    override this.ToString() =
        match this with
        | ShowUsage -> ""
        | EmptyAssemblyPaths -> "Please specify the assemblies to generate code for"
        | InvalidAssemblyPath path -> sprintf "The path to the assembly '%s' is invalid" path
        | InvalidOutputFile file -> sprintf "The path to the output file is invalid '%s'" file
        | NoOutputFile -> "Please specify the path to the output file"
        | UnknownOption opt -> sprintf "Unknown option or invalid argument '%s'" opt

[<RequireQualifiedAccess>]
module Arguments =
    type StateType =
        private
        | AssemblyPaths
        | ExcludeAssemblyFiles
        | OutputFile
        | Invalid of InvalidArgument
        | LaunchDebugger
        | Unknown
    and private State =
        { AssemblyPaths: Path list
          Exclude: Excluded
          LaunchDebugger: bool
          OutputFile: Path option
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
            Optional "help", Invalid ShowUsage, "Shows this help message", ""
            // TODO: Make this option accept directories as well.
            Required "assembly-paths", AssemblyPaths, "Specifies the paths to the assemblies", "path list"
            Optional "exclude-assembly-files", ExcludeAssemblyFiles, "Specifies the names of the assembly files to exclude from code generation", "name list"
            Optional "launch-debugger", LaunchDebugger, "Calls Debugger.Launch after all arguments have been processed", ""
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
            | (LaunchDebugger, _) ->
                let state' = { state with LaunchDebugger = true; Type = Unknown }
                inner state' args
            | (_, []) ->
                match state with
                | { AssemblyPaths = phead :: ptail
                    OutputFile = Some out } ->
                    { AssemblyPaths = phead, ptail
                      Exclude = state.Exclude
                      LaunchDebugger = state.LaunchDebugger
                      OutputFile = out }
                    |> Ok
                | { AssemblyPaths = [] } -> Error EmptyAssemblyPaths
                | { OutputFile = None } -> Error NoOutputFile
            | (_, arg :: tail) ->
                let state' =
                    match (arg, state.Type) with
                    | (Argument arg', _) -> { state with Type = arg'.State }
                    | (Path.Valid path, AssemblyPaths) ->
                        { state with AssemblyPaths = path :: state.AssemblyPaths }
                    | (path, AssemblyPaths) -> // TODO: Maybe create function for failing fast by returning { state with Type = ... |> Invalid }
                        { state with Type = InvalidAssemblyPath path |> Invalid }
                    | (file, ExcludeAssemblyFiles) ->
                        { state with Exclude = { state.Exclude with AssemblyFiles = Set.add file state.Exclude.AssemblyFiles } }
                    | (Path.Valid path, OutputFile) ->
                        { state with OutputFile = Some path; Type = Unknown }
                    | (path, OutputFile) -> // TODO: Check that the path is a file and not a directory.
                        { state with Type = InvalidOutputFile path |> Invalid }
                    | _ ->
                        { state with Type = UnknownOption arg |> Invalid }
                inner state' tail
        inner
            { AssemblyPaths = []
              Exclude = { AssemblyFiles = Set.empty }
              LaunchDebugger = false
              OutputFile = None
              Type = Unknown }
