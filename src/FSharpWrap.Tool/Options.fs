namespace FSharpWrap.Tool

type Options =
    { AssemblyPaths: Path * Path list
      Filter: Filter
      LaunchDebugger: bool
      OutputFile: File }

    member this.Assemblies =
        let h, tail = this.AssemblyPaths in h :: tail

type InvalidOptions =
    | EmptyAssemblyPaths
    | InvalidAssemblyPath of string
    | InvalidOutputFile of string
    | MixedFilter
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
        | MixedFilter -> "Cannot both include in and exclude from in code generation"
        | MultipleOutputFiles -> "Please specify only one output file"
        | NoOutputFile -> "Please specify the path to the output file"
        | InvalidArgument arg -> sprintf "Invalid argument '%s'" arg
        | InvalidOption opt -> sprintf "Invalid option specified '--%s'" opt

[<RequireQualifiedAccess>]
module Options =
    type StateType =
        private
        | AssemblyPaths
        | AssemblyNames of (Set<string> -> FilterType<string>)
        | Invalid of InvalidOptions
        | LaunchDebugger
        | OutputFile
        | Unknown

    type private State =
        { AssemblyPaths: Path list
          Filter: Filter
          LaunchDebugger: bool
          OutputFile: File option
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
            "exclude-assembly-names", AssemblyNames Exclude, "Specifies the names of the assembly files to exclude from code generation", ArgumentList "names"
            "help", Invalid ShowUsage, "Shows this help message", Switch
            "include-assembly-names", AssemblyNames Include, "Specifies the names of the assembly files to include in code generation", ArgumentList "names"
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
            match state, args with
            | { Type = Invalid msg }, _ ->
                Error msg
            | { Type = OutputFile; OutputFile = Some _ }, _ ->
                Error MultipleOutputFiles
            | { Type = LaunchDebugger }, _ ->
                let state' = { state with LaunchDebugger = true; Type = Unknown }
                inner state' args
            | _, [] ->
                match state with
                | { AssemblyPaths = phead :: ptail
                    OutputFile = out } ->
                    let out' =
                        match out with
                        | Some file -> file
                        | None -> File.ofStr "output.autogen.fs" |> Option.get
                    { AssemblyPaths = phead, ptail
                      Filter = state.Filter
                      LaunchDebugger = state.LaunchDebugger
                      OutputFile = out' }
                    |> Ok
                | { AssemblyPaths = [] } -> Error EmptyAssemblyPaths
            | _, arg :: tail ->
                let inline invalid err = { state with Type = Invalid err }
                let state' =
                    match arg, state.Type with
                    | (ValidOption arg', _) -> { state with Type = arg'.State }
                    | (InvalidOption opt, _) -> InvalidOption opt |> invalid
                    | (Path.Valid path, AssemblyPaths) ->
                        { state with AssemblyPaths = path :: state.AssemblyPaths }
                    | (File.Valid file, OutputFile) ->
                        { state with OutputFile = Some file; Type = Unknown }
                    | (name, AssemblyNames filter) -> // TODO: What would a valid assembly name look like?
                        let names =
                            Set.add
                                name
                                state.Filter.Assemblies.Items
                            |> filter
                        match state.Filter.Assemblies, names with
                        | Exclude _, Include _
                        | Include _, Exclude _ ->
                            invalid MixedFilter
                        | _ ->
                            { state with Filter = { state.Filter with Assemblies = names } }
                    | (path, AssemblyPaths) -> InvalidAssemblyPath path |> invalid
                    | (path, OutputFile) -> InvalidOutputFile path |> invalid
                    | _ -> InvalidArgument arg |> invalid
                inner state' tail
        inner
            { AssemblyPaths = []
              Filter = Filter.Empty
              LaunchDebugger = false
              OutputFile = None
              Type = AssemblyPaths }
