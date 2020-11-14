namespace FSharpWrap.Tool.Benchmarks

open BenchmarkDotNet.Attributes
open BenchmarkDotNet.Engines

open FSharpWrap.Tool
open FSharpWrap.Tool.Reflection
open FSharpWrap.Tool.Generation

module Data =
    let inline assemblies() =
        let ctx = Context.init { Excluded.AssemblyFiles = Set.empty }
        AssemblyInfo.ofAssembly
            (typeof<System.Collections.Immutable.ImmutableDictionary>.Assembly)
            ctx
        |> fst
        |> List.singleton

[<MemoryDiagnoser>]
type Benchmarks() =
    let consumer = Consumer()
    let assms = Data.assemblies()
    let file = Generate.fromAssemblies assms

    [<Benchmark>]
    member _.Reflect() = Data.assemblies()
    [<Benchmark>]
    member _.GenerateCode() = Generate.fromAssemblies assms
    [<Benchmark>]
    member _.PrintCode() = (Print.genFile file).Consume consumer
