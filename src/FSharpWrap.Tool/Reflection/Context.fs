namespace FSharpWrap.Tool.Reflection

open System
open System.Collections.Immutable

type Context =
    private
        { Types: ImmutableDictionary<Type, TypeArg> }

type ContextExpr<'T> = Context -> 'T * Context

[<RequireQualifiedAccess>]
module private Context =
    let empty =
        { Types = ImmutableDictionary.Empty }

    let map mapping (expr: ContextExpr<_>) =
        fun ctx ->
            let value, ctx' = expr ctx
            mapping value, ctx'
    let retn value: ContextExpr<_> = fun ctx -> value, ctx
    let types: ContextExpr<_> = fun ctx -> ctx.Types, ctx

[<AutoOpen>]
module private ContextBuilder =
    type ContextBuilder() =
        // member _.Bind(exp: Expr<_>, body: _ -> Expr<_>): Expr<_>
        member _.Bind(expr: ContextExpr<_>, body: _ -> ContextExpr<_>) =
            fun ctx -> expr ctx ||> body
        member _.Return obj: ContextExpr<_> = fun ctx -> obj, ctx
        member _.ReturnFrom expr: ContextExpr<_> = expr

    let context = ContextBuilder()
