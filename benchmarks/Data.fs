namespace FSharpWrap.Tool.Benchmarks

open System
open System.Reflection

open FSharpWrap.Tool.Reflection

type RuntimeAssemblyResolver() =
    inherit MetadataAssemblyResolver()

    override _.Resolve(_, name) =
        AppDomain.CurrentDomain.GetAssemblies()
        |> Array.tryFind (fun assm -> name = assm.GetName())
        |> Option.toObj

[<RequireQualifiedAccess>]
module Data =
    let assemblies =
        let r = RuntimeAssemblyResolver()
        let assms =
            [
                typeof<System.Collections.Immutable.ImmutableDictionary>
            ]
            |> List.map (fun t -> t.Assembly.GetName())
        fun() ->
            Reflect.context
                r
                (fun ctx ->
                    assms |> List.map ctx.LoadFromAssemblyName)
                { AssemblyFiles = Set.empty }
