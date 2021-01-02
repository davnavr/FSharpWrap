﻿namespace FSharpWrap.Tool

open System
open System.Collections.Generic
open System.Reflection

/// Stores the names of types and namespaces.
[<Sealed>]
type NameCache(assms: Assembly[]) =
    let names = assms.Length * 8 |> Dictionary<Type, TypeName>
    let namespaces = assms.Length |> Dictionary<string, Namespace>
    let types = assms.Length * 12 |> Dictionary<Type, TypeArg> // TODO: Should TypeName instances not be stored here to avoid redundancy and reduce the amount of allocations?

    member _.GetNamespace (ns: string) =
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
            let gargs', parent =
                let gargs' = Array.map this.GetTypeArg gargs
                match t.DeclaringType with
                | null -> gargs', None
                | parent' ->
                    let parent'' = this.GetTypeName parent'
                    let gargs'' =
                        Array.where
                            (fun garg -> Array.contains garg parent''.TypeArgs |> not)
                            gargs'
                    gargs'', Some parent''
            let name =
                { Name = FsName.ofType t
                  Namespace = this.GetNamespace t.Namespace
                  Parent = parent
                  TypeArgs = gargs' }
            names.Item <- t, name
            name

    member this.GetTypeArg (t: Type) =
        match types.TryGetValue t with
        | true, existing -> existing
        | false, _ ->
            let t' =
                match t with
                | GenericParam constraints ->
                    { Constraints =
                        lazy
                            // TODO: How are different types of constraints handled?
                            Array.map
                                (fun ct -> this.GetTypeArg ct |> TypeConstraint)
                                constraints
                            |> Set.ofArray
                        |> GenericConstraints
                      ParamName = FsName.ofType t }
                    |> TypeParam 
                | IsArray elem ->
                    {| ElementType = this.GetTypeArg elem
                       Rank = t.GetArrayRank() |> uint |}
                    |> ArrayType
                | IsByRef tref -> this.GetTypeArg tref |> ByRefType
                | IsPointer pnt -> this.GetTypeArg pnt |> PointerType
                | _ -> this.GetTypeName t |> TypeName
            types.Item <- t, t'
            t'
