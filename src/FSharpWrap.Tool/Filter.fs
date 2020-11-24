namespace FSharpWrap.Tool

open System.Reflection

type FilterType<'Item when 'Item : comparison> =
    | Exclude of Set<'Item>
    | Include of Set<'Item>
    | All

    member this.Items =
        match this with
        | Exclude items
        | Include items -> items
        | All -> Set.empty

type Filter =
    { Assemblies: FilterType<string>
      Namespaces: FilterType<Namespace> }

    static member Empty =
        { Assemblies = All
          Namespaces = All }

[<RequireQualifiedAccess>]
module Filter =
    let private all _ = true

    let assemblyIncluded filter =
        let found names (assm: Assembly) =
            let name = assm.GetName().Name
            Set.contains name names
        match filter.Assemblies with
        | Exclude exc -> found exc >> not
        | Include inc -> found inc
        | All -> all
