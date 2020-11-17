module FSharpWrap.Tool.Benchmarks

open System.IO
open System.Reflection
open System.Text

open BenchmarkDotNet.Attributes
open BenchmarkDotNet.Configs
open BenchmarkDotNet.Running

open FSharpWrap.Tool
open FSharpWrap.Tool.Reflection
open FSharpWrap.Tool.Generation

let inline private assemblies() =
    Context.init
        { Excluded.AssemblyFiles = Set.empty }
    |> AssemblyInfo.ofAssembly
        (typeof<System.Collections.Immutable.ImmutableDictionary>.Assembly)
    |> List.singleton

let private sw() = lazy(new StreamWriter(new MemoryStream()))

type PrintData =
    { Name: string
      Print: Printer }

    override this.ToString() = this.Name

[<GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByMethod)>]
[<MemoryDiagnoser>]
[<MinColumn; MaxColumn>]
type Benchmarks() =
    let assms = assemblies()
    let file = Generate.fromAssemblies assms
    let out =
        { Close = ignore
          Line = ignore
          Write = ignore }

    [<Benchmark>]
    member _.Reflect() = assemblies()
    [<Benchmark>]
    member _.GenerateCode() = Generate.fromAssemblies assms
    [<Benchmark>]
    member _.PrintCode() = Print.genFile file out

[<EntryPoint>]
let main argv =
    let assm = Assembly.GetExecutingAssembly()
    BenchmarkSwitcher
        .FromAssembly(assm)
        .Run(args = argv)
    |> ignore
    0
