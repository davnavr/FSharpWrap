namespace FSharpWrap.Tool

[<RequireQualifiedAccess>]
type AssemblyFiles =
    | Exclude of Set<File>
    | Include of Set<File>
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
