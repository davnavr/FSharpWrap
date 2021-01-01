namespace FSharpWrap.Tool

open System
open System.Collections.Generic
open System.Reflection

/// Stores the names of types and namespaces.
[<Sealed>]
type NameCache(assms: Assembly[]) =
    let names = assms.Length * 8 |> Dictionary<Type, TypeName>
    let namespaces = assms.Length |> Dictionary<string, Namespace>
    let types = assms.Length * 12 |> Dictionary<Type, TypeArg> // TODO: Should TypeName instances not be stored here to avoid redundancy and reduce the amount of allocations?

    member this.GetNamespace (ns: string) =
        match namespaces.TryGetValue ns with
        | true, existing -> existing
        | false, _ ->
            let ns' = Namespace.ofStr ns
            namespaces.Item <- ns, ns'
            ns'

    member this.GetTypeName (GenericArgs gargs as t) =
        match names.TryGetValue t with
        | true, existing -> existing
        | false, _ ->
            let parent = Option.ofObj t.DeclaringType
            let name =
                { Name = FsName.ofType t
                  Namespace = this.GetNamespace t.Namespace
                  Parent = Option.map (this.GetTypeName) parent
                  TypeArgs =
                    match parent with
                    | None -> gargs
                    | Some (GenericArgs inherited) ->
                        Array.where
                            (fun garg -> Array.contains garg inherited |> not)
                            gargs
                    |> Array.map this.GetTypeArg }
            names.Item <- t, name
            name

    member this.GetTypeArg (t: Type) =
        match types.TryGetValue t with
        | true, existing -> existing
        | false, _ ->
            let t' =
                match t with
                | GenericParam constraints ->
                    TypeParam { ParamName = FsName.ofType t }
                | IsArray elem ->
                    {| ElementType = this.GetTypeArg elem
                       Rank = t.GetArrayRank() |> uint |}
                    |> ArrayType
                | IsByRef tref -> this.GetTypeArg tref |> ByRefType
                | IsPointer pnt -> this.GetTypeArg pnt |> PointerType
                | _ -> this.GetTypeName t |> TypeName
            types.Item <- t, t'
            t'
