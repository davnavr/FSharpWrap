namespace FSharpWrap.Tool

type Filter =
    { ExcludeAssemblyFiles: Set<Path>
      ExcludeAssemblyNames: Set<string>
      IncludeAssemblyFiles: Set<Path>
      IncludeAssemblyNames: Set<string> }

    static member Empty =
        { ExcludeAssemblyFiles = Set.empty
          ExcludeAssemblyNames = Set.empty
          IncludeAssemblyFiles = Set.empty
          IncludeAssemblyNames = Set.empty }

type Options =
    { AssemblyPaths: Path * Path list
      Filter: Filter
      LaunchDebugger: bool
      OutputFile: Path }

    member this.Assemblies =
        let h, tail = this.AssemblyPaths in h :: tail

type InvalidOptions =
    | EmptyAssemblyPaths
    | InvalidAssemblyPath of string
    | InvalidOutputFile of string
    | MultipleOutputFiles
    | NoOutputFile
    | ShowUsage
    | InvalidArgument of string
    | InvalidOption of string

    override this.ToString() =
        match this with
        | ShowUsage -> ""
        | EmptyAssemblyPaths -> "Please specify the assemblies to generate code for"
        | InvalidAssemblyPath path -> sprintf "The path to the assembly '%s' is invalid" path
        | InvalidOutputFile file -> sprintf "The path to the output file is invalid '%s'" file
        | MultipleOutputFiles -> "Please specify only one output file"
        | NoOutputFile -> "Please specify the path to the output file"
        | InvalidArgument arg -> sprintf "Invalid argument '%s'" arg
        | InvalidOption opt -> sprintf "Invalid option specified '--%s'" opt

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
          Filter: Filter
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
        let (|ValidOption|InvalidOption|Argument|) =
            function
            | (opt: string) when opt.StartsWith "--" ->
                let name =
                    opt
                        .Substring(2)
                        .TrimEnd()
                match Map.tryFind name all with
                | Some opt' -> Choice1Of3 opt'
                | None -> Choice2Of3 name
            | arg -> Choice3Of3 arg
        let rec inner state args =
            match (state.Type, args) with
            | (Invalid msg, _) -> Error msg
            | (OutputFile, _) when state.OutputFile.IsSome -> Error MultipleOutputFiles
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
                      Filter = state.Filter
                      LaunchDebugger = state.LaunchDebugger
                      OutputFile = out' }
                    |> Ok
                | { AssemblyPaths = [] } -> Error EmptyAssemblyPaths
            | (_, arg :: tail) ->
                let inline invalid err = { state with Type = Invalid err }
                let state' =
                    match (arg, state.Type) with
                    | (ValidOption arg', _) -> { state with Type = arg'.State }
                    | (InvalidOption opt, _) -> InvalidOption opt |> invalid
                    | (Path.Valid path, AssemblyPaths) ->
                        { state with AssemblyPaths = path :: state.AssemblyPaths }
                    | (Path.Valid file, ExcludeAssemblyFiles) -> // TODO: Check that it is a file.
                        let excluded = Set.add file state.Filter.ExcludeAssemblyFiles
                        { state with Filter = { state.Filter with ExcludeAssemblyFiles = excluded } }
                    | (Path.Valid file, IncludeAssemblyFiles) ->
                        let included = Set.add file state.Filter.IncludeAssemblyFiles
                        { state with Filter = { state.Filter with IncludeAssemblyFiles = included } }
                    | (Path.Valid file, OutputFile) ->
                        { state with OutputFile = Some file; Type = Unknown }
                    | (name, ExcludeAssemblyNames) -> // TODO: What would a valid assembly name look like?
                        let excluded = Set.add name state.Filter.ExcludeAssemblyNames
                        { state with Filter = { state.Filter with ExcludeAssemblyNames = excluded } }
                    | (name, IncludeAssemblyNames) ->
                        let included = Set.add name state.Filter.IncludeAssemblyNames
                        { state with Filter = { state.Filter with IncludeAssemblyNames = included } }
                    | (path, AssemblyPaths)
                    | (path, ExcludeAssemblyFiles)
                    | (path, IncludeAssemblyFiles) -> InvalidAssemblyPath path |> invalid
                    | (path, OutputFile) -> InvalidOutputFile path |> invalid // TODO: Check that the path is a file and not a directory.
                    | _ -> InvalidArgument arg |> invalid
                inner state' tail
        inner
            { AssemblyPaths = []
              Filter = Filter.Empty
              LaunchDebugger = false
              OutputFile = None
              Type = AssemblyPaths }
