namespace FSharpWrap.Tool

open System.Reflection

[<RequireQualifiedAccess>]
type AssemblyFiles =
    | Exclude of Set<Path>
    | Include of Set<Path>
    | All

[<RequireQualifiedAccess>]
type AssemblyNames =
    | Exclude of Set<string>
    | Include of Set<string>
    | All

type Filter =
    { AssemblyFiles: AssemblyFiles
      AssemblyNames: AssemblyNames
      Namespaces: Set<Namespace> }

    static member Empty =
        { AssemblyFiles = AssemblyFiles.All
          AssemblyNames = AssemblyNames.All
          Namespaces = Set.empty }

[<RequireQualifiedAccess>]
module Filter =
    let assemblyFiles { AssemblyFiles = files } =
        match files with
        | AssemblyFiles.Exclude files'
        | AssemblyFiles.Include files' -> files'
        | AssemblyFiles.All -> Set.empty

    let assemblyNames { AssemblyNames = names } =
        match names with
        | AssemblyNames.Exclude names'
        | AssemblyNames.Include names' -> names'
        | AssemblyNames.All -> Set.empty

    let private filterLocation files convert =
        fun (assm: Assembly) ->
            match Path.ofStr assm.Location with
            | Some path -> Set.contains path files |> convert
            | None -> true

    let assemblyIncluded filter =
        match filter.AssemblyFiles with
        | AssemblyFiles.Include inc -> filterLocation inc id
        | AssemblyFiles.Exclude exc -> filterLocation exc not
        | AssemblyFiles.All -> fun _ -> true
        // TODO: Check assembly names
