namespace FSharpWrap.Tool

type Excluded =
    { AssemblyFiles: Set<Path>
      AssemblyNames: Set<string> }

type Options =
    { AssemblyPaths: Path * Path list
      Exclude: Excluded
      LaunchDebugger: bool
      OutputFile: Path }

    member this.Assemblies =
        let h, tail = this.AssemblyPaths in h :: tail

type InvalidOptions =
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
module Options =
    type StateType =
        private
        | AssemblyPaths
        | ExcludeAssemblyFiles
        | ExcludeAssemblyNames
        | IncludeAssemblyFiles
        | IncludeAssemblyNames
        | Invalid of InvalidOptions
        | LaunchDebugger
        | OutputFile
        | Unknown

    type private State =
        { AssemblyPaths: Path list
          Exclude: Excluded
          LaunchDebugger: bool
          OutputFile: Path option
          Type: StateType }

    type OptionType =
        | Switch
        | Argument of string
        | ArgumentList of string

    type Info =
        { ArgType: OptionType
          Description: string
          State: StateType }

    let all =
        // TODO: Make some options accept directories and wildcards as well.
        [
            "exclude-assembly-files", ExcludeAssemblyFiles, "Specifies the paths to the assemblies to exclude from code generation", ArgumentList "paths"
            "exclude-assembly-names", ExcludeAssemblyNames, "Specifies the names of the assembly files to exclude from code generation", ArgumentList "names"
            "help", Invalid ShowUsage, "Shows this help message", Switch
            "include-assembly-files", IncludeAssemblyFiles, "Specifies the paths to the assemblies to include in code generation", ArgumentList "paths"
            "include-assembly-names", IncludeAssemblyNames, "Specifies the names of the assembly files to include in code generation", ArgumentList "names"
            "launch-debugger", LaunchDebugger, "Calls Debugger.Launch after all arguments have been processed", Switch
            "output-file", OutputFile, "Specifies the path to the file containing the generated F# code", Argument "file"
        ]
        |> Seq.map (fun (name, st, desc, argt) ->
            name,
            { ArgType = argt
              Description = desc
              State = st })
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
                    OutputFile = out } ->
                    let out' =
                        match out with
                        | Some file -> file
                        | None -> Path.ofStr "output.autogen.fs" |> Option.get
                    { AssemblyPaths = phead, ptail
                      Exclude = state.Exclude
                      LaunchDebugger = state.LaunchDebugger
                      OutputFile = out' }
                    |> Ok
                | { AssemblyPaths = [] } -> Error EmptyAssemblyPaths
            | (_, arg :: tail) ->
                let inline invalid err = { state with Type = Invalid err }
                let state' =
                    match (arg, state.Type) with
                    | (Argument arg', _) -> { state with Type = arg'.State }
                    | (Path.Valid path, AssemblyPaths) ->
                        { state with AssemblyPaths = path :: state.AssemblyPaths }
                    | (Path.Valid file, ExcludeAssemblyFiles) -> // TODO: Check that it is a file.
                        { state with Exclude = { state.Exclude with AssemblyFiles = Set.add file state.Exclude.AssemblyFiles } }
                    | (path, ExcludeAssemblyFiles)
                    | (path, AssemblyPaths) -> InvalidAssemblyPath path |> invalid
                    | (Path.Valid file, OutputFile) ->
                        { state with OutputFile = Some file; Type = Unknown }
                    | (path, OutputFile) -> InvalidOutputFile path |> invalid // TODO: Check that the path is a file and not a directory.
                    | _ -> UnknownOption arg |> invalid
                inner state' tail
        inner
            { AssemblyPaths = []
              Exclude =
                { AssemblyFiles = Set.empty
                  AssemblyNames = Set.empty }
              LaunchDebugger = false
              OutputFile = None
              Type = AssemblyPaths }
