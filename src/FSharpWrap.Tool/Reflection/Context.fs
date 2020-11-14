namespace FSharpWrap.Tool.Reflection

open System
open System.Collections.Immutable

open FSharpWrap.Tool

type Context =
    private
        { Excluded: Excluded
          TypeParams: ImmutableDictionary<Type, TypeParam>
          TypeRefs: ImmutableDictionary<Type, TypeRef> }

    member this.Filter = this.Excluded

type ContextExpr<'T> = Context -> 'T * Context

[<AutoOpen>]
module private ContextPatterns =
    let (|HasType|_|) (t: Type) ctx =
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
          TypeParams = ImmutableDictionary.Empty
          TypeRefs = ImmutableDictionary.Empty }

    let map mapping (expr: ContextExpr<_>) ctx =
        let value, ctx' = expr ctx
        mapping value, ctx'
    let retn value: ContextExpr<_> = fun ctx -> value, ctx
    let current (ctx: Context) = ctx, ctx

[<AutoOpen>]
module private ContextBuilder =
    type ContextBuilder() =
        member _.Bind(expr: ContextExpr<_>, body: _ -> ContextExpr<_>) =
            fun ctx -> expr ctx ||> body
        member _.Bind(update: Context -> Context, body: unit -> ContextExpr<_>) =
            update >> body()
        member _.Return obj: ContextExpr<_> = fun ctx -> obj, ctx
        member _.ReturnFrom expr: ContextExpr<_> = expr

    let context = ContextBuilder()
