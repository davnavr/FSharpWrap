module FSharpWrap.Tool.Benchmarks

open System.Reflection

open BenchmarkDotNet.Attributes
open BenchmarkDotNet.Configs
open BenchmarkDotNet.Engines
open BenchmarkDotNet.Running

open FSharpWrap.Tool
open FSharpWrap.Tool.Reflection
open FSharpWrap.Tool.Generation

let inline private assemblies() =
    let ctx = Context.init { Excluded.AssemblyFiles = Set.empty }
    AssemblyInfo.ofAssembly
        (typeof<System.Collections.Immutable.ImmutableDictionary>.Assembly)
        ctx
    |> fst
    |> List.singleton

[<GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByMethod)>]
[<MemoryDiagnoser>]
[<MinColumn; MaxColumn>]
type Benchmarks() =
    let consumer = Consumer()
    let assms = assemblies()
    let file = Generate.fromAssemblies assms

    [<Benchmark>]
    member _.Reflect() = assemblies()
    [<Benchmark>]
    member _.GenerateCode() = Generate.fromAssemblies assms
    [<Benchmark>]
    member _.PrintCode() = (Print.genFile file).Consume consumer

[<EntryPoint>]
let main argv =
    (Assembly.GetExecutingAssembly() |> BenchmarkSwitcher.FromAssembly).Run(args = argv)
    |> ignore
    0
