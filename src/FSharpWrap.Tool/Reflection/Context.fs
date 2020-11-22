namespace FSharpWrap.Tool.Reflection

open System
open System.Collections.Generic

open FSharpWrap.Tool

type Context =
    private
        { Excluded: Filter
          TypeParams: Dictionary<Type, TypeParam>
          TypeRefs: Dictionary<Type, TypeRef> }

    member this.Filter = this.Excluded

[<AutoOpen>]
module internal ContextPatterns =
    let (|HasType|_|) (t: Type, ctx) =
        match t, ctx with
        | GenericParam _, { TypeParams = ContainsValue t tparam } ->
            TypeParam tparam |> Some
        | GenericArg, { TypeRefs = ContainsValue t tref } ->
            TypeArg tref |> Some
        | _ -> None

[<RequireQualifiedAccess>]
module Context =
    let init filter =
        { Excluded = filter
          TypeParams = Dictionary<_, _>()
          TypeRefs = Dictionary<_, _>() }
