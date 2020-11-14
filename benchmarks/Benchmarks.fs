namespace FSharpWrap.Tool.Benchmarks

open BenchmarkDotNet.Attributes
open BenchmarkDotNet.Engines

open FSharpWrap.Tool.Generation

[<MemoryDiagnoser>]
type ReflectionBenchmarks() =
    [<Benchmark>]
    member _.Reflect() = Data.assemblies()

[<MemoryDiagnoser>]
type GenerationBenchmarks() =
    let assms = Data.assemblies()

    [<Benchmark>]
    member _.GenerateCode() = Generate.fromAssemblies assms

[<MemoryDiagnoser>]
type PrintBenchmarks() =
    let data = Data.assemblies() |> Generate.fromAssemblies
    let consumer = Consumer()

    [<Benchmark>]
    member _.PrintCode() = (Print.genFile data).Consume consumer
