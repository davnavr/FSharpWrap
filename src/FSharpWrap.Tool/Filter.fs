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

    let inline private included items map =
        let found items' item =
            let item' = map item
            Set.contains item' items'
        match items with
        | Exclude exc -> found exc >> not
        | Include inc -> found inc
        | All -> all

    let assemblyIncluded { Assemblies = assms } =
        fun (assm: Assembly) ->
            assm.GetName().Name
        |> included assms

    let typeIncluded { Namespaces = namespaces } =
        fun (t: System.Type) ->
            Namespace.ofStr t.Namespace
        |> included namespaces
