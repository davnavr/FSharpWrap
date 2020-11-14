module FSharpWrap.Tool.Benchmarks.Runner

open System.Reflection

open BenchmarkDotNet.Running

[<EntryPoint>]
let main argv =
    (Assembly.GetExecutingAssembly() |> BenchmarkSwitcher.FromAssembly).Run(args = argv)
    |> ignore
    0
