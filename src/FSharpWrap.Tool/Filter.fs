namespace FSharpWrap.Tool

[<RequireQualifiedAccess>]
type AssemblyFiles =
    | Exclude of Set<File>
    | Include of Set<File>
    | All

type AssemblyNamesFilter =
    | ExcludeAssemblyNames of Set<string>
    | IncludeAssemblyNames of Set<string>

type Filter =
    { AssemblyFiles: AssemblyFiles
      AssemblyNames: AssemblyNamesFilter
      Namespaces: Set<Namespace> }

    static member Empty =
        { AssemblyFiles = AssemblyFiles.All
          AssemblyNames = ExcludeAssemblyNames Set.empty
          Namespaces = Set.empty }

[<RequireQualifiedAccess>]
module Filter =
    let addAssemblyName name filter =
        let names =
            let add = Set.add name
            match filter.AssemblyNames with
            | ExcludeAssemblyNames files -> add files |> ExcludeAssemblyNames
            | IncludeAssemblyNames files -> add files |> IncludeAssemblyNames
        { filter with AssemblyNames = names }

    let assemblyFiles { AssemblyFiles = files } =
        match files with
        | AssemblyFiles.Exclude files'
        | AssemblyFiles.Include files' -> files'
        | AssemblyFiles.All -> Set.empty
