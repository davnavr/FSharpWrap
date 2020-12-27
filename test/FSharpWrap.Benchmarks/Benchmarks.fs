module FSharpWrap.Tool.Benchmarks

open System.IO
open System.Reflection

open BenchmarkDotNet.Attributes
open BenchmarkDotNet.Configs
open BenchmarkDotNet.Running

open FSharpWrap.Tool

[<GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByMethod)>]
[<MemoryDiagnoser>]
[<MinColumn; MaxColumn>]
type Benchmarks() =
    let assemblies = [ typeof<System.Collections.Immutable.ImmutableDictionary>.Assembly ]

    [<Benchmark>]
    member _.PrintCode() =
        use printer = new Print.Printer(new StreamWriter(Stream.Null))
        Generate.fromAssemblies assemblies Filter.Empty printer

[<EntryPoint>]
let main argv =
    let assm = Assembly.GetExecutingAssembly()
    BenchmarkSwitcher
        .FromAssembly(assm)
        .Run(args = argv)
    |> ignore
    0
